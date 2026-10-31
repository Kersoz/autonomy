using Autonomy.Application.Abstractions.Persistence;
using Autonomy.Domain.Entities;
using Autonomy.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Autonomy.Persistence.Repositories;

public sealed class DeliveryRepository(AutonomyDbContext context) : IDeliveryRepository
{
    public Task<Delivery?> GetByIdAsync(
        Guid deliveryId,
        CancellationToken cancellationToken = default)
    {
        return context.Deliveries.SingleOrDefaultAsync(
            delivery => delivery.Id == deliveryId,
            cancellationToken);
    }

    public Task<bool> MerchantBelongsToFleetAsync(
        Guid merchantId,
        Guid fleetId,
        CancellationToken cancellationToken = default)
    {
        return context.Merchants.AnyAsync(
            merchant => merchant.Id == merchantId && merchant.FleetId == fleetId,
            cancellationToken);
    }

    public Task<Guid?> GetMerchantTenantIdAsync(
        Guid merchantId,
        CancellationToken cancellationToken = default)
    {
        return context.Merchants
            .Where(merchant => merchant.Id == merchantId)
            .Select(merchant => (Guid?)merchant.TenantId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> CourierBelongsToFleetAsync(
        Guid courierId,
        Guid fleetId,
        CancellationToken cancellationToken = default)
    {
        return context.Couriers.AnyAsync(
            courier => courier.Id == courierId && courier.FleetId == fleetId,
            cancellationToken);
    }

    public Task<bool> IsAssignedCourierAsync(
        Guid deliveryId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return context.Deliveries.AnyAsync(
            delivery => delivery.Id == deliveryId
                && delivery.AssignedCourier != null
                && delivery.AssignedCourier.UserId == userId,
            cancellationToken);
    }

    public void Add(Delivery delivery)
    {
        context.Deliveries.Add(delivery);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
