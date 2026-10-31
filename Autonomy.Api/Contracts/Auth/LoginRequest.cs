using System.ComponentModel.DataAnnotations;

namespace Autonomy.Api.Contracts.Auth;

/// <summary>Credentials and optional tenant context for signing in.</summary>
public sealed record LoginRequest
{
    /// <summary>The account email address.</summary>
    [Required, EmailAddress]
    public required string Email { get; init; }

    /// <summary>The account password.</summary>
    [Required]
    public required string Password { get; init; }

    /// <summary>The fleet tenant to activate for this session, when applicable.</summary>
    public Guid? TenantId { get; init; }

    /// <summary>The merchant to scope access to for MerchantUser sessions.</summary>
    public Guid? MerchantId { get; init; }
}
