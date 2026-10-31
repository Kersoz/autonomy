using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Autonomy.Api.Authorization;
using Autonomy.Api.Contracts.Auth;
using Autonomy.Persistence.Context;
using Autonomy.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Autonomy.Api.Services;

public sealed class AuthService(
    AutonomyDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IConfiguration configuration)
{
    private const int AccessTokenMinutes = 15;
    private const int RefreshTokenDays = 30;

    private readonly string _issuer = configuration["Authentication:Jwt:Issuer"]
        ?? throw new InvalidOperationException("JWT issuer is not configured.");
    private readonly string _audience = configuration["Authentication:Jwt:Audience"]
        ?? throw new InvalidOperationException("JWT audience is not configured.");
    private readonly SymmetricSecurityKey _signingKey = CreateSigningKey(
        configuration["Authentication:Jwt:Key"]);
    private readonly int _accessTokenMinutes =
        GetPositiveConfigurationValue(
            configuration,
            "Authentication:Jwt:AccessTokenMinutes",
            AccessTokenMinutes);
    private readonly int _refreshTokenDays =
        GetPositiveConfigurationValue(
            configuration,
            "Authentication:Jwt:RefreshTokenDays",
            RefreshTokenDays);

    public async Task<AuthLoginResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return AuthLoginResult.InvalidCredentials;
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            return AuthLoginResult.InvalidCredentials;
        }

        var scope = await ResolveScopeAsync(
            user,
            request.TenantId,
            request.MerchantId,
            cancellationToken);
        if (scope.RequiresTenantSelection)
        {
            return AuthLoginResult.TenantSelectionRequired;
        }

        if (scope.Roles.Count == 0)
        {
            return AuthLoginResult.InvalidCredentials;
        }

        return new AuthLoginResult(
            await CreateSessionAsync(user, scope, cancellationToken),
            false,
            false);
    }

    public async Task<AuthSession?> RefreshAsync(
        string rawRefreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(rawRefreshToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        var existingToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash,
                cancellationToken);
        var now = DateTime.UtcNow;
        if (existingToken is null
            || existingToken.RevokedAtUtc is not null
            || existingToken.ExpiresAtUtc <= now)
        {
            return null;
        }

        var user = await userManager.FindByIdAsync(existingToken.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var scope = await ResolveScopeAsync(
            user,
            existingToken.TenantId,
            existingToken.MerchantId,
            cancellationToken);
        if (scope.RequiresTenantSelection || scope.Roles.Count == 0)
        {
            existingToken.RevokedAtUtc = now;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var rawNewToken = GenerateRefreshToken();
        var newTokenHash = HashToken(rawNewToken);
        var updatedCount = await dbContext.RefreshTokens
            .Where(token =>
                token.Id == existingToken.Id
                && token.RevokedAtUtc == null
                && token.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(token => token.RevokedAtUtc, now)
                    .SetProperty(token => token.ReplacedByTokenHash, newTokenHash),
                cancellationToken);

        if (updatedCount == 0)
        {
            return null;
        }

        var session = CreateSessionTokens(user, scope, rawNewToken, now);
        dbContext.RefreshTokens.Add(session.RefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return session;
    }

    public async Task RevokeAsync(
        Guid userId,
        string rawRefreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(rawRefreshToken);
        var now = DateTime.UtcNow;
        await dbContext.RefreshTokens
            .Where(token =>
                token.UserId == userId
                && token.TokenHash == tokenHash
                && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(token => token.RevokedAtUtc, now),
                cancellationToken);
    }

    private async Task<AuthScope> ResolveScopeAsync(
        ApplicationUser user,
        Guid? tenantId,
        Guid? merchantId,
        CancellationToken cancellationToken)
    {
        var globalRoles = await userManager.GetRolesAsync(user);
        var isPlatformAdmin = globalRoles.Contains(AuthorizationRoles.PlatformAdmin);

        if (!tenantId.HasValue && !merchantId.HasValue)
        {
            return isPlatformAdmin
                ? new AuthScope(globalRoles.ToArray(), null, null, false)
                : new AuthScope([], null, null, true);
        }

        if (!tenantId.HasValue
            || !await dbContext.Fleets.IgnoreQueryFilters().AnyAsync(
                fleet => fleet.Id == tenantId.Value,
                cancellationToken))
        {
            return new AuthScope([], null, null, false);
        }

        if (merchantId.HasValue
            && !await dbContext.Merchants.IgnoreQueryFilters().AnyAsync(
                merchant => merchant.Id == merchantId.Value
                    && merchant.FleetId == tenantId.Value,
                cancellationToken))
        {
            return new AuthScope([], null, null, false);
        }

        if (isPlatformAdmin)
        {
            return new AuthScope(globalRoles.ToArray(), tenantId, merchantId, false);
        }

        var tenantRoles = await dbContext.TenantRoleAssignments
            .IgnoreQueryFilters()
            .Where(assignment =>
                assignment.UserId == user.Id
                && assignment.TenantId == tenantId.Value
                && assignment.MerchantId == merchantId
                && assignment.IsActive)
            .Select(assignment => assignment.RoleName)
            .Distinct()
            .ToListAsync(cancellationToken);

        tenantRoles = tenantRoles
            .Where(role => AuthorizationPolicies.IsRoleAllowedForTenant(role)
                && ((role == AuthorizationRoles.MerchantUser) == merchantId.HasValue))
            .ToList();

        return new AuthScope(tenantRoles, tenantId, merchantId, false);
    }

    private async Task<AuthSession> CreateSessionAsync(
        ApplicationUser user,
        AuthScope scope,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var rawRefreshToken = GenerateRefreshToken();
        var session = CreateSessionTokens(user, scope, rawRefreshToken, now);
        dbContext.RefreshTokens.Add(session.RefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }

    private AuthSession CreateSessionTokens(
        ApplicationUser user,
        AuthScope scope,
        string rawRefreshToken,
        DateTime now)
    {
        var expiresAt = now.AddMinutes(_accessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, user.UserName ?? user.Email ?? user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        claims.AddRange(scope.Roles.Distinct().Select(role =>
            new Claim("role", role)));

        if (scope.TenantId.HasValue)
        {
            claims.Add(new Claim(
                AuthorizationPolicies.TenantIdClaim,
                scope.TenantId.Value.ToString()));
        }

        if (scope.MerchantId.HasValue)
        {
            claims.Add(new Claim(
                AuthorizationPolicies.MerchantIdClaim,
                scope.MerchantId.Value.ToString()));
        }

        var credentials = new SigningCredentials(
            _signingKey,
            SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            _issuer,
            _audience,
            claims,
            now,
            expiresAt,
            credentials);

        var response = new AccessTokenResponse(
            new JwtSecurityTokenHandler().WriteToken(jwt),
            new DateTimeOffset(expiresAt, TimeSpan.Zero),
            user.Id,
            user.Email ?? string.Empty,
            scope.Roles.Distinct().ToArray(),
            scope.TenantId,
            scope.MerchantId);

        var refreshExpiresAt = now.AddDays(_refreshTokenDays);
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(rawRefreshToken),
            TenantId = scope.TenantId,
            MerchantId = scope.MerchantId,
            CreatedAtUtc = now,
            ExpiresAtUtc = refreshExpiresAt
        };

        return new AuthSession(response, rawRefreshToken, refreshExpiresAt, refreshToken);
    }

    private static SymmetricSecurityKey CreateSigningKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must be configured with at least 32 bytes.");
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }

    private static int GetPositiveConfigurationValue(
        IConfiguration configuration,
        string key,
        int defaultValue)
    {
        var value = configuration.GetValue(key, defaultValue);
        if (value <= 0)
        {
            throw new InvalidOperationException($"{key} must be greater than zero.");
        }

        return value;
    }

    private static string GenerateRefreshToken()
    {
        return Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(64));
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public sealed record AuthSession(
        AccessTokenResponse AccessToken,
        string RefreshTokenValue,
        DateTime RefreshTokenExpiresAtUtc,
        RefreshToken RefreshToken);

    public sealed record AuthLoginResult(
        AuthSession? Session,
        bool RequiresTenantSelection,
        bool IsInvalidCredentials)
    {
        public static AuthLoginResult InvalidCredentials { get; } = new(null, false, true);
        public static AuthLoginResult TenantSelectionRequired { get; } = new(null, true, false);
    }

    private sealed record AuthScope(
        IReadOnlyList<string> Roles,
        Guid? TenantId,
        Guid? MerchantId,
        bool RequiresTenantSelection);
}
