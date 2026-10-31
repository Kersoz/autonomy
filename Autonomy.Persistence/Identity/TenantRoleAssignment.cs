using Autonomy.Domain.Common;

namespace Autonomy.Persistence.Identity;

public sealed class TenantRoleAssignment : ITenantEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public Guid TenantId { get; set; }

    public Guid? MerchantId { get; set; }

    public string RoleName { get; set; } = null!;

    public bool IsActive { get; set; } = true;
}
