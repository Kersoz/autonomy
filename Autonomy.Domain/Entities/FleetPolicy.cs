using Autonomy.Domain.Common;

namespace Autonomy.Domain.Entities;

public class FleetPolicy : BaseEntity
{
    public Guid FleetId { get; set; }

    public string Version { get; set; } = null!;

    public bool MerchantCancellationAllowed { get; set; }

    public int? CancellationWindowMinutes { get; set; }

    public bool MerchantTrackingEnabled { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public string? ChangeReason { get; set; }
}