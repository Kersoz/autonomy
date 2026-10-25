using Autonomy.Domain.Common;
using Autonomy.Domain.Enums;

namespace Autonomy.Domain.Entities;

public class Delivery : BaseEntity
{
    public string ReferenceNumber { get; set; } = null!;

    public Guid FleetId { get; set; }

    public Fleet Fleet { get; set; } = null!;

    public Guid MerchantId { get; set; }

    public Merchant Merchant { get; set; } = null!;

    public string CreationSource { get; set; } = "Manual";

    public string CustomerName { get; set; } = null!;

    public string CustomerPhone { get; set; } = null!;

    public string PickupAddress { get; set; } = null!;

    public decimal PickupLatitude { get; set; }

    public decimal PickupLongitude { get; set; }

    public string DropoffAddress { get; set; } = null!;

    public decimal DropoffLatitude { get; set; }

    public decimal DropoffLongitude { get; set; }

    public decimal OrderValue { get; set; }
    public decimal? DeliveryCharge { get; set; }
    public decimal? CourierCompensation { get; set; }
    public string Currency { get; set; } = "TRY";

    public PaymentMethod PaymentMethod { get; set; }

    public DeliveryStatus Status { get; set; }

    public Guid? AssignedCourierId { get; set; }

    public Courier? AssignedCourier { get; set; }

    public Guid? ActiveAssignmentId { get; set; }

    public Assignment? ActiveAssignment { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public string? MerchantOrderReference { get; set; }

    public string? PickupInstructions { get; set; }

    public string? DeliveryInstructions { get; set; }

    public string? InternalNotes { get; set; }

    public DateTime? FirstAssignmentAt { get; set; }

    public DateTime? TravelToPickupStartedAt { get; set; }

    public DateTime? ArrivedAtMerchantAt { get; set; }

    public DateTime? PickedUpAt { get; set; }

    public DateTime? TravelToCustomerStartedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancellationReason { get; set; }

    public string? CompletionMethod { get; set; }

    public string? CompletionActor { get; set; }
}