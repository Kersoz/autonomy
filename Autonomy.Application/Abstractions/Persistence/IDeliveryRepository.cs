using Autonomy.Domain.Entities;

namespace Autonomy.Application.Abstractions.Persistence;

public interface IDeliveryRepository
{
    Task<Delivery?> GetByIdAsync(
        Guid deliveryId,
        CancellationToken cancellationToken = default);

    Task<bool> MerchantBelongsToFleetAsync(
        Guid merchantId,
        Guid fleetId,
        CancellationToken cancellationToken = default);

    Task<bool> CourierBelongsToFleetAsync(
        Guid courierId,
        Guid fleetId,
        CancellationToken cancellationToken = default);

    Task<bool> IsAssignedCourierAsync(
        Guid deliveryId,
        Guid userId,
        CancellationToken cancellationToken = default);

    void Add(Delivery delivery);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
