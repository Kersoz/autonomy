using System.Security.Claims;
using Autonomy.Api.Middleware;
using Autonomy.Application.Interfaces;
using Autonomy.Persistence.Identity;

namespace Autonomy.Api.Authorization;

public sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    : ITenantContext
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? TenantId => GetGuidClaim(AuthorizationPolicies.TenantIdClaim);

    public Guid? MerchantId => GetGuidClaim(AuthorizationPolicies.MerchantIdClaim);

    public Guid? UserId => GetGuidClaim("sub");

    public bool IsPlatformAdmin =>
        User?.IsInRole(AuthorizationRoles.PlatformAdmin) == true;

    public string? CorrelationId =>
        httpContextAccessor.HttpContext?.Items[CorrelationIdMiddleware.ItemKey] as string;

    private Guid? GetGuidClaim(string claimType)
    {
        return Guid.TryParse(User?.FindFirst(claimType)?.Value, out var value)
            ? value
            : null;
    }
}
