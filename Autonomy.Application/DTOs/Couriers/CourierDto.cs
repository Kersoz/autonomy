using Autonomy.Domain.Enums;

namespace Autonomy.Application.DTOs.Couriers;

public class CourierDto
{
    public Guid Id { get; set; }

    public Guid FleetId { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public CourierOnlineStatus OnlineStatus { get; set; }

    public CourierAvailabilityStatus AvailabilityStatus { get; set; }

    public DateTime? LastConnectionAt { get; set; }

    public string? VehicleType { get; set; }

    public string? VehiclePlate { get; set; }

    public Guid? CurrentAssignmentId { get; set; }

    public int ActiveDeliveryCount { get; set; }

    public int MaxSimultaneousDeliveries { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
