namespace Autonomy.Api.Contracts.Auth;

/// <summary>Account details and roles visible to the requesting administrator.</summary>
public sealed record UserDetailsResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    IReadOnlyList<UserRoleAssignmentResponse> Roles);

/// <summary>A role assigned to a user globally or within a tenant.</summary>
public sealed record UserRoleAssignmentResponse(
    string RoleName,
    Guid? TenantId,
    Guid? MerchantId);
