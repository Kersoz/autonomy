using System.ComponentModel.DataAnnotations;

namespace Autonomy.Api.Contracts.Auth;

/// <summary>Creates an account and assigns a platform or tenant-scoped role.</summary>
public sealed record CreateUserRequest
{
    /// <summary>The new user's email address and sign-in name.</summary>
    [Required, EmailAddress, MaxLength(256)]
    public required string Email { get; init; }

    /// <summary>The user's initial password, communicated securely to them.</summary>
    [Required, MinLength(10)]
    public required string Password { get; init; }

    /// <summary>The user's given name.</summary>
    [Required, MaxLength(100)]
    public required string FirstName { get; init; }

    /// <summary>The user's family name.</summary>
    [Required, MaxLength(100)]
    public required string LastName { get; init; }

    /// <summary>The role to assign to the new account.</summary>
    [Required]
    public required string RoleName { get; init; }

    /// <summary>The tenant identifier for tenant-scoped roles.</summary>
    public Guid? TenantId { get; init; }

    /// <summary>The merchant identifier for MerchantUser accounts.</summary>
    public Guid? MerchantId { get; init; }
}
