namespace Autonomy.Api.Contracts.Auth;

/// <summary>A user's role assignment within a fleet, optionally scoped to a merchant.</summary>
public sealed record TenantRoleAssignmentResponse(
    Guid Id,
    Guid UserId,
    Guid TenantId,
    Guid? MerchantId,
    string RoleName);
