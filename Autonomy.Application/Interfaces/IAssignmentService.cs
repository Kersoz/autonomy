using Autonomy.Application.Features.Deliveries;

namespace Autonomy.Application.Interfaces;

public interface IAssignmentService
{
    Task<DeliveryDto?> AssignCourierAsync(
        Guid deliveryId,
        Guid courierId,
        CancellationToken cancellationToken = default);

    Task<DeliveryDto?> ReleaseAssignmentAsync(
        Guid deliveryId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<DeliveryDto?> ReassignCourierAsync(
        Guid deliveryId,
        Guid courierId,
        CancellationToken cancellationToken = default);

    Task<DeliveryDto?> PickupAsync(
        Guid deliveryId,
        Guid? courierUserId,
        CancellationToken cancellationToken = default);

    Task<DeliveryDto?> CompleteDeliveryAsync(
        Guid deliveryId,
        Guid? courierUserId,
        CancellationToken cancellationToken = default);

    Task<DeliveryDto?> CancelDeliveryAsync(
        Guid deliveryId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<DeliveryDto?> FailAsync(
        Guid deliveryId,
        Guid? courierUserId,
        CancellationToken cancellationToken = default);
}
