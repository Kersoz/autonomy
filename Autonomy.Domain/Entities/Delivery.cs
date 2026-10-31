using System.ComponentModel.DataAnnotations.Schema;
using Autonomy.Domain.Common;
using Autonomy.Domain.Enums;
using Autonomy.Domain.Exceptions;

namespace Autonomy.Domain.Entities;

public class Delivery : BaseEntity
{
    private readonly List<DeliveryStatusTransition> _statusTransitions = [];

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

    public DeliveryStatus Status { get; private set; } = DeliveryStatus.Created;

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

    [NotMapped]
    public IReadOnlyCollection<DeliveryStatusTransition> PendingStatusTransitions =>
        _statusTransitions.AsReadOnly();

    public void ClearPendingStatusTransitions()
    {
        _statusTransitions.Clear();
    }

    public void QueueForAssignment()
    {
        TransitionTo(DeliveryStatus.PendingAssignment, DeliveryStatus.Created);
    }

    public void Assign(Guid courierId)
    {
        if (courierId == Guid.Empty)
        {
            throw new ArgumentException("A courier ID is required.", nameof(courierId));
        }

        TransitionTo(DeliveryStatus.Assigned, DeliveryStatus.PendingAssignment);
        AssignedCourierId = courierId;
        FirstAssignmentAt ??= DateTime.UtcNow;
    }

    public void Reassign(Guid courierId)
    {
        if (courierId == Guid.Empty)
        {
            throw new ArgumentException("A courier ID is required.", nameof(courierId));
        }

        if (Status != DeliveryStatus.Assigned)
        {
            throw new InvalidDeliveryTransitionException(Status, DeliveryStatus.Assigned);
        }

        AssignedCourierId = courierId;
    }

    public void ReleaseAssignment()
    {
        TransitionTo(DeliveryStatus.PendingAssignment, DeliveryStatus.Assigned);
        AssignedCourierId = null;
        ClearActiveAssignment();
    }

    public void ClearActiveAssignment()
    {
        ActiveAssignment = null;
        ActiveAssignmentId = null;
    }

    public void Pickup()
    {
        TransitionTo(
            DeliveryStatus.PickedUp,
            DeliveryStatus.Assigned,
            DeliveryStatus.CourierOnTheWayToPickup,
            DeliveryStatus.ArrivedAtMerchant);
        PickedUpAt = DateTime.UtcNow;
    }

    public void Deliver(string? completionActor = null)
    {
        TransitionTo(
            DeliveryStatus.Delivered,
            DeliveryStatus.PickedUp,
            DeliveryStatus.CourierOnTheWayToCustomer);
        DeliveredAt = DateTime.UtcNow;
        CompletionMethod = "Courier";
        CompletionActor = completionActor;
    }

    public void Cancel(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A cancellation reason is required.", nameof(reason));
        }

        TransitionTo(
            DeliveryStatus.Cancelled,
            DeliveryStatus.Created,
            DeliveryStatus.PendingAssignment,
            DeliveryStatus.Assigned,
            DeliveryStatus.CourierOnTheWayToPickup,
            DeliveryStatus.ArrivedAtMerchant);
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason.Trim();
    }

    public void Fail()
    {
        TransitionTo(
            DeliveryStatus.Failed,
            DeliveryStatus.Created,
            DeliveryStatus.PendingAssignment,
            DeliveryStatus.Assigned,
            DeliveryStatus.CourierOnTheWayToPickup,
            DeliveryStatus.ArrivedAtMerchant,
            DeliveryStatus.PickedUp,
            DeliveryStatus.CourierOnTheWayToCustomer);
    }

    private void TransitionTo(
        DeliveryStatus targetStatus,
        params DeliveryStatus[] allowedCurrentStatuses)
    {
        if (!allowedCurrentStatuses.Contains(Status))
        {
            throw new InvalidDeliveryTransitionException(Status, targetStatus);
        }

        var previousStatus = Status;
        Status = targetStatus;
        _statusTransitions.Add(new DeliveryStatusTransition(
            Guid.NewGuid(),
            previousStatus,
            targetStatus,
            DateTime.UtcNow));
    }
}