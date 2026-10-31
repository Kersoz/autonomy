using Autonomy.Domain.Common;
using Autonomy.Domain.Enums;

namespace Autonomy.Domain.Entities;

public class Courier : BaseEntity
{
    public Guid FleetId { get; set; }

    public Fleet Fleet { get; set; } = null!;

    public Guid UserId { get; set; }

    public string Name { get; set; } = null!;

    public string ContactPhone { get; set; } = null!;

    public bool IsEnabled { get; set; }

    public CourierOnlineStatus OnlineStatus { get; set; }

    public CourierAvailabilityStatus AvailabilityStatus { get; set; }

    public DateTime? LastConnectionAt { get; set; }

    public string? VehicleType { get; set; }

    public string? VehiclePlate { get; set; }

    public Guid? CurrentAssignmentId { get; set; }

    public int ActiveDeliveryCount { get; set; }

    public int MaxSimultaneousDeliveries { get; set; } = 1;

    public void GoOnline(DateTime lastSeenAt)
    {
        OnlineStatus = CourierOnlineStatus.Online;
        LastConnectionAt = lastSeenAt;
    }

    public void GoOffline(DateTime lastSeenAt)
    {
        OnlineStatus = CourierOnlineStatus.Offline;
        LastConnectionAt = lastSeenAt;
    }

    public void SetAvailable(DateTime lastSeenAt)
    {
        AvailabilityStatus = CourierAvailabilityStatus.Available;
        LastConnectionAt = lastSeenAt;
    }

    public void SetUnavailable(DateTime lastSeenAt)
    {
        AvailabilityStatus = CourierAvailabilityStatus.Unavailable;
        LastConnectionAt = lastSeenAt;
    }
}