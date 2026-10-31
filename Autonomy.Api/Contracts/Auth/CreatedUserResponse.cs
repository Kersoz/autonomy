namespace Autonomy.Api.Contracts.Auth;

/// <summary>New account and its assigned access scope.</summary>
public sealed record CreatedUserResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string RoleName,
    Guid? TenantId,
    Guid? MerchantId);
