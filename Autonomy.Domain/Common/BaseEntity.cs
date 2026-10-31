namespace Autonomy.Domain.Common;

public abstract class BaseEntity
    : ITenantEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? UpdatedBy { get; set; }
}