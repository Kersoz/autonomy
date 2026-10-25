using Autonomy.Domain.Common;
using Autonomy.Domain.Enums;

namespace Autonomy.Domain.Entities;

public class Merchant : BaseEntity
{
    public Guid FleetId { get; set; }

    public Fleet Fleet { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string ContactPhone { get; set; } = null!;

    public string PickupAddress { get; set; } = null!;

    public decimal PickupLatitude { get; set; }

    public decimal PickupLongitude { get; set; }

    public bool IsEnabled { get; set; }

    public MerchantStatus OperatingStatus { get; set; }

    public string? ContactPerson { get; set; }

    public string? Email { get; set; }

    public string? PickupInstructions { get; set; }

    public ICollection<Delivery> Deliveries { get; set; }
        = new List<Delivery>();
}