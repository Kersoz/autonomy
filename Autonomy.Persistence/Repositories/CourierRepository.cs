using Autonomy.Application.Abstractions.Persistence;
using Autonomy.Domain.Entities;
using Autonomy.Domain.Enums;
using Autonomy.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Autonomy.Persistence.Repositories;

public class CourierRepository : GenericRepository<Courier>, ICourierRepository
{
    public CourierRepository(AutonomyDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<Courier>> GetAllByFleetAsync(
        Guid fleetId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Couriers
            .AsNoTracking()
            .Where(courier => courier.FleetId == fleetId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CourierDispatchSnapshot>> GetDispatchCandidatesAsync(
        Guid fleetId,
        CancellationToken cancellationToken = default)
    {
        var couriers = await Context.Couriers
            .AsNoTracking()
            .Where(courier => courier.FleetId == fleetId
                && courier.IsEnabled
                && courier.OnlineStatus == CourierOnlineStatus.Online
                && courier.AvailabilityStatus == CourierAvailabilityStatus.Available
                && courier.ActiveDeliveryCount < courier.MaxSimultaneousDeliveries)
            .ToListAsync(cancellationToken);
        if (couriers.Count == 0)
        {
            return [];
        }

        var courierIds = couriers.Select(courier => courier.Id).ToArray();
        var latestLocations = await Context.CourierLocations
            .AsNoTracking()
            .Where(location => location.FleetId == fleetId
                && courierIds.Contains(location.CourierId))
            .GroupBy(location => location.CourierId)
            .Select(group => group
                .OrderByDescending(location => location.RecordedAt)
                .ThenByDescending(location => location.Id)
                .First())
            .ToDictionaryAsync(location => location.CourierId, cancellationToken);

        return couriers
            .Select(courier => new CourierDispatchSnapshot(
                courier,
                latestLocations.GetValueOrDefault(courier.Id)))
            .ToArray();
    }

    public Task<Courier?> GetByIdInFleetAsync(
        Guid id,
        Guid fleetId,
        CancellationToken cancellationToken = default)
    {
        return Context.Couriers
            .SingleOrDefaultAsync(
                courier => courier.Id == id && courier.FleetId == fleetId,
                cancellationToken);
    }

    public async Task<Courier> SaveStateAsync(
        Courier courier,
        CancellationToken cancellationToken = default)
    {
        await Context.SaveChangesAsync(cancellationToken);
        return courier;
    }

    public async Task<bool> DeleteByIdInFleetAsync(
        Guid id,
        Guid fleetId,
        CancellationToken cancellationToken = default)
    {
        var courier = await Context.Couriers
            .SingleOrDefaultAsync(
                item => item.Id == id && item.FleetId == fleetId,
                cancellationToken);
        if (courier is null)
        {
            return false;
        }

        Context.Couriers.Remove(courier);
        await Context.SaveChangesAsync(cancellationToken);
        return true;
    }
}