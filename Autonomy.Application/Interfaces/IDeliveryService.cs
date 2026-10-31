using Autonomy.Application.Features.Deliveries;

namespace Autonomy.Application.Interfaces;

public interface IDeliveryService
{
    Task<DeliveryDto> CreateDeliveryAsync(
        CreateDeliveryCommand command,
        CancellationToken cancellationToken = default);

    Task<DeliveryDto?> GetByIdAsync(
        Guid deliveryId,
        CancellationToken cancellationToken = default);

    Task<DeliveryDto?> AssignAsync(
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
        CancellationToken cancellationToken = default);

    Task<DeliveryDto?> CompleteDeliveryAsync(
        Guid deliveryId,
        CancellationToken cancellationToken = default);

    Task<DeliveryDto?> CancelDeliveryAsync(
        Guid deliveryId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<DeliveryDto?> FailAsync(
        Guid deliveryId,
        CancellationToken cancellationToken = default);
}
