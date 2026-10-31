namespace Autonomy.Application.DTOs.Couriers;

public class CreateCourierDto
{
    public Guid FleetId { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;

    public string? VehicleType { get; set; }

    public string? VehiclePlate { get; set; }

    public int MaxSimultaneousDeliveries { get; set; } = 1;
}
