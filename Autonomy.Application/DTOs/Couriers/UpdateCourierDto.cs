namespace Autonomy.Application.DTOs.Couriers;

public class UpdateCourierDto
{
    public string Name { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public string? VehicleType { get; set; }

    public string? VehiclePlate { get; set; }

    public int MaxSimultaneousDeliveries { get; set; } = 1;
}
