using Autonomy.Application.DTOs.Couriers;

namespace Autonomy.Application.Interfaces;

public interface ICourierService
{
    Task<IReadOnlyList<CourierDto>> GetAllAsync(
        Guid? fleetId = null,
        CancellationToken cancellationToken = default);

    Task<CourierDto?> GetByIdAsync(
        Guid id,
        Guid? fleetId = null,
        CancellationToken cancellationToken = default);

    Task<CourierDto> CreateAsync(
        CreateCourierDto courier,
        CancellationToken cancellationToken = default);

    Task<CourierDto?> UpdateAsync(
        Guid id,
        UpdateCourierDto courier,
        Guid? fleetId = null,
        CancellationToken cancellationToken = default);

    Task<CourierDto?> GoOnlineAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CourierDto?> GoOfflineAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CourierDto?> SetAvailableAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CourierDto?> SetUnavailableAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        Guid? fleetId = null,
        CancellationToken cancellationToken = default);
}
