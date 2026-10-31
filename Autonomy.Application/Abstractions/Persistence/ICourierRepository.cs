using Autonomy.Domain.Entities;

namespace Autonomy.Application.Abstractions.Persistence;

public interface ICourierRepository : IGenericRepository<Courier>
{
    Task<IReadOnlyList<Courier>> GetAllByFleetAsync(
        Guid fleetId,
        CancellationToken cancellationToken = default);

    Task<Courier?> GetByIdInFleetAsync(
        Guid id,
        Guid fleetId,
        CancellationToken cancellationToken = default);

    Task<Courier> SaveStateAsync(
        Courier courier,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteByIdInFleetAsync(
        Guid id,
        Guid fleetId,
        CancellationToken cancellationToken = default);
}
