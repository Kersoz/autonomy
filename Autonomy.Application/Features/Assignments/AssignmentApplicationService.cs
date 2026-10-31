using Autonomy.Application.Abstractions.Persistence;
using Autonomy.Application.Features.Deliveries;
using Autonomy.Application.Interfaces;
using Autonomy.Domain.Entities;
using Autonomy.Domain.Enums;
using Autonomy.Domain.Exceptions;

namespace Autonomy.Application.Features.Assignments;

public sealed class AssignmentApplicationService(
    IAssignmentRepository assignmentRepository,
    ITenantContext tenantContext) : IAssignmentService
{
    public Task<DeliveryDto?> AssignCourierAsync(
        Guid deliveryId,
        Guid courierId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            deliveryId,
            courierId,
            transaction =>
            {
                var (delivery, courier) = RequireTargetCourier(transaction, courierId);
                if (transaction.ActiveAssignment is not null)
                {
                    throw new AssignmentConflictException(
                        "The delivery already has an active assignment.");
                }

                if (delivery.Status != DeliveryStatus.PendingAssignment)
                {
                    throw new InvalidDeliveryTransitionException(
                        delivery.Status,
                        DeliveryStatus.Assigned);
                }

                delivery.Assign(courierId);
                var assignment = CreateAssignment(delivery, courier);
                delivery.ActiveAssignment = assignment;
                delivery.ActiveAssignmentId = assignment.Id;
                return delivery;
            },
            cancellationToken);
    }

    public Task<DeliveryDto?> ReleaseAssignmentAsync(
        Guid deliveryId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A release reason is required.", nameof(reason));
        }

        return ExecuteAsync(
            deliveryId,
            null,
            transaction =>
            {
                var (delivery, assignment, _) = RequireActiveAssignment(transaction);
                delivery.ReleaseAssignment();
                EndAssignment(delivery, assignment, AssignmentStatus.Released, reason.Trim());
                return delivery;
            },
            cancellationToken);
    }

    public Task<DeliveryDto?> ReassignCourierAsync(
        Guid deliveryId,
        Guid courierId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            deliveryId,
            courierId,
            transaction =>
            {
                var (delivery, currentAssignment, currentCourier) =
                    RequireActiveAssignment(transaction);
                var (_, newCourier) = RequireTargetCourier(transaction, courierId);
                if (currentCourier.Id == newCourier.Id)
                {
                    throw new AssignmentConflictException(
                        "The delivery is already assigned to this courier.");
                }

                delivery.Reassign(courierId);
                EndAssignment(
                    delivery,
                    currentAssignment,
                    AssignmentStatus.Reassigned,
                    "Reassigned");

                var newAssignment = CreateAssignment(delivery, newCourier);
                newAssignment.PreviousAssignmentId = currentAssignment.Id;
                delivery.ActiveAssignment = newAssignment;
                delivery.ActiveAssignmentId = newAssignment.Id;
                return delivery;
            },
            cancellationToken);
    }

    public Task<DeliveryDto?> PickupAsync(
        Guid deliveryId,
        Guid? courierUserId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            deliveryId,
            null,
            transaction =>
            {
                var (delivery, _, _) = RequireActiveAssignment(
                    transaction,
                    courierUserId,
                    requireCourierUser: true);
                delivery.Pickup();
                return delivery;
            },
            cancellationToken);
    }

    public Task<DeliveryDto?> CompleteDeliveryAsync(
        Guid deliveryId,
        Guid? courierUserId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            deliveryId,
            null,
            transaction =>
            {
                var (delivery, assignment, _) =
                    RequireActiveAssignment(
                        transaction,
                        courierUserId,
                        requireCourierUser: true);
                delivery.Deliver(courierUserId?.ToString());
                EndAssignment(delivery, assignment, AssignmentStatus.Completed, "Completed");
                return delivery;
            },
            cancellationToken);
    }

    public Task<DeliveryDto?> CancelDeliveryAsync(
        Guid deliveryId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            deliveryId,
            null,
            transaction =>
            {
                var delivery = transaction.Delivery;
                if (delivery is null)
                {
                    return null;
                }

                delivery.Cancel(reason);
                if (transaction.ActiveAssignment is { } assignment)
                {
                    EndAssignment(delivery, assignment, AssignmentStatus.Cancelled, reason.Trim());
                }

                return delivery;
            },
            cancellationToken);
    }

    public Task<DeliveryDto?> FailAsync(
        Guid deliveryId,
        Guid? courierUserId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            deliveryId,
            null,
            transaction =>
            {
                var (delivery, assignment, _) =
                    RequireActiveAssignment(
                        transaction,
                        courierUserId,
                        requireCourierUser: true);
                delivery.Fail();
                EndAssignment(delivery, assignment, AssignmentStatus.Cancelled, "Failed");
                return delivery;
            },
            cancellationToken);
    }

    private Task<DeliveryDto?> ExecuteAsync(
        Guid deliveryId,
        Guid? targetCourierId,
        Func<AssignmentTransaction, Delivery?> operation,
        CancellationToken cancellationToken)
    {
        return ExecuteAndMapAsync(
            deliveryId,
            targetCourierId,
            operation,
            cancellationToken);
    }

    private async Task<DeliveryDto?> ExecuteAndMapAsync(
        Guid deliveryId,
        Guid? targetCourierId,
        Func<AssignmentTransaction, Delivery?> operation,
        CancellationToken cancellationToken)
    {
        var delivery = await assignmentRepository.ExecuteAtomicallyAsync(
            deliveryId,
            targetCourierId,
            operation,
            cancellationToken);
        return delivery is null ? null : ToDto(delivery);
    }

    private (Delivery Delivery, Courier Courier) RequireTargetCourier(
        AssignmentTransaction transaction,
        Guid courierId)
    {
        var delivery = transaction.Delivery
            ?? throw new KeyNotFoundException("The delivery was not found.");
        if (!transaction.Couriers.TryGetValue(courierId, out var courier)
            || courier.FleetId != delivery.FleetId)
        {
            throw new KeyNotFoundException("The courier was not found in the delivery fleet.");
        }

        if (!courier.IsEnabled)
        {
            throw new AssignmentConflictException("The courier is disabled.");
        }

        if (courier.OnlineStatus != CourierOnlineStatus.Online)
        {
            throw new AssignmentConflictException("The courier is offline.");
        }

        if (courier.AvailabilityStatus != CourierAvailabilityStatus.Available)
        {
            throw new AssignmentConflictException("The courier is unavailable.");
        }

        if (courier.ActiveDeliveryCount >= courier.MaxSimultaneousDeliveries)
        {
            throw new AssignmentConflictException(
                "The courier has reached concurrent assignment capacity.");
        }

        return (delivery, courier);
    }

    private (Delivery Delivery, Assignment Assignment, Courier Courier) RequireActiveAssignment(
        AssignmentTransaction transaction,
        Guid? courierUserId = null,
        bool requireCourierUser = false)
    {
        var delivery = transaction.Delivery
            ?? throw new KeyNotFoundException("The delivery was not found.");
        var assignment = transaction.ActiveAssignment
            ?? throw new AssignmentConflictException(
                "The delivery does not have an active assignment.");
        if (!transaction.Couriers.TryGetValue(assignment.CourierId, out var courier))
        {
            throw new InvalidOperationException(
                "The active assignment references a courier that does not exist.");
        }

        if (requireCourierUser
            && (courierUserId is not Guid userId || courier.UserId != userId))
        {
            throw new UnauthorizedAccessException(
                "Only the courier assigned to this delivery can update its status.");
        }

        return (delivery, assignment, courier);
    }

    private Assignment CreateAssignment(Delivery delivery, Courier courier)
    {
        return new Assignment
        {
            Id = Guid.NewGuid(),
            FleetId = delivery.FleetId,
            Delivery = delivery,
            DeliveryId = delivery.Id,
            Courier = courier,
            CourierId = courier.Id,
            AssignedAt = DateTime.UtcNow,
            Source = AssignmentSource.DispatcherAction,
            Status = AssignmentStatus.Active,
            DispatchOrigin = DispatchOrigin.Manual,
            SelectedBy = tenantContext.UserId?.ToString()
        };
    }

    private void EndAssignment(
        Delivery delivery,
        Assignment assignment,
        AssignmentStatus status,
        string reason)
    {
        assignment.Status = status;
        assignment.EndedAt = DateTime.UtcNow;
        assignment.ReleaseReason = reason;
        assignment.ReleasedBy = tenantContext.UserId?.ToString();
        delivery.ClearActiveAssignment();
    }

    private static DeliveryDto ToDto(Delivery delivery)
    {
        return new DeliveryDto(
            delivery.Id,
            delivery.ReferenceNumber,
            delivery.FleetId,
            delivery.MerchantId,
            delivery.Status,
            delivery.AssignedCourierId,
            new DateTimeOffset(DateTime.SpecifyKind(delivery.CreatedAt, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(delivery.UpdatedAt, DateTimeKind.Utc)));
    }
}
