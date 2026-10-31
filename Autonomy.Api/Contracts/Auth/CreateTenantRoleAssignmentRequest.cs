using System.ComponentModel.DataAnnotations;

namespace Autonomy.Api.Contracts.Auth;

/// <summary>Assigns an application role to a user within one tenant.</summary>
public sealed record CreateTenantRoleAssignmentRequest
{
    /// <summary>The Identity user receiving the tenant role.</summary>
    [Required]
    public required Guid UserId { get; init; }

    /// <summary>The fleet tenant identifier.</summary>
    [Required]
    public required Guid TenantId { get; init; }

    /// <summary>The merchant identifier when assigning MerchantUser.</summary>
    public Guid? MerchantId { get; init; }

    /// <summary>The tenant-scoped role to assign.</summary>
    [Required]
    public required string RoleName { get; init; }
}
