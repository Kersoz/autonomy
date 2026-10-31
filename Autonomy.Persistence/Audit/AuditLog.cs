using Autonomy.Domain.Common;

namespace Autonomy.Persistence.Audit;

public sealed class AuditLog : ITenantEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string EntityName { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string ChangesJson { get; set; } = null!;

    public Guid? ActorUserId { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public string? CorrelationId { get; set; }
}
