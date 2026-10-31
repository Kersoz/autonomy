namespace Autonomy.Api.Contracts.Auth;

/// <summary>Access token and active authorization context returned after authentication.</summary>
public sealed record AccessTokenResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles,
    Guid? TenantId,
    Guid? MerchantId);
