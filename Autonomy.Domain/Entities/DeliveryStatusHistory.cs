using Autonomy.Domain.Common;

namespace Autonomy.Domain.Entities;

public class DeliveryStatusHistory : BaseEntity
{
    public Guid DeliveryId { get; set; }

    public string PreviousStatus { get; set; } = null!;

    public string NewStatus { get; set; } = null!;

    public Guid? ChangedByUserId { get; set; }

    public DateTime ChangedAt { get; set; }
}