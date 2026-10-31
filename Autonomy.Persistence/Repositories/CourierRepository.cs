using Autonomy.Application.Abstractions.Persistence;
using Autonomy.Domain.Entities;
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