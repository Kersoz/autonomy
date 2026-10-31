using AutoMapper;
using Autonomy.Application.Abstractions.Persistence;
using Autonomy.Application.DTOs.Couriers;
using Autonomy.Application.Interfaces;
using Autonomy.Domain.Entities;

namespace Autonomy.Application.Features.Couriers;

public class CourierService : ICourierService
{
    private readonly ICourierRepository _courierRepository;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenantContext;

    public CourierService(
        ICourierRepository courierRepository,
        IMapper mapper,
        ITenantContext tenantContext)
    {
        _courierRepository = courierRepository;
        _mapper = mapper;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<CourierDto>> GetAllAsync(
        Guid? fleetId = null,
        CancellationToken cancellationToken = default)
    {
        var couriers = fleetId.HasValue
            ? await _courierRepository.GetAllByFleetAsync(fleetId.Value, cancellationToken)
            : await _courierRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<List<CourierDto>>(couriers);
    }

    public async Task<CourierDto?> GetByIdAsync(
        Guid id,
        Guid? fleetId = null,
        CancellationToken cancellationToken = default)
    {
        var courier = fleetId.HasValue
            ? await _courierRepository.GetByIdInFleetAsync(
                id,
                fleetId.Value,
                cancellationToken)
            : await _courierRepository.GetByIdAsync(id, cancellationToken);
        return courier is null ? null : _mapper.Map<CourierDto>(courier);
    }

    public async Task<CourierDto> CreateAsync(
        CreateCourierDto courier,
        CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<Courier>(courier);
        var createdCourier = await _courierRepository.AddAsync(entity, cancellationToken);
        return _mapper.Map<CourierDto>(createdCourier);
    }

    public async Task<CourierDto?> UpdateAsync(
        Guid id,
        UpdateCourierDto courier,
        Guid? fleetId = null,
        CancellationToken cancellationToken = default)
    {
        var entity = fleetId.HasValue
            ? await _courierRepository.GetByIdInFleetAsync(
                id,
                fleetId.Value,
                cancellationToken)
            : await _courierRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        _mapper.Map(courier, entity);
        entity.UpdatedAt = DateTime.UtcNow;

        var updatedCourier = await _courierRepository.UpdateAsync(entity, cancellationToken);
        return _mapper.Map<CourierDto>(updatedCourier);
    }

    public Task<CourierDto?> GoOnlineAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return UpdateStateAsync(id, courier => courier.GoOnline(DateTime.UtcNow), cancellationToken);
    }

    public Task<CourierDto?> GoOfflineAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return UpdateStateAsync(id, courier => courier.GoOffline(DateTime.UtcNow), cancellationToken);
    }

    public Task<CourierDto?> SetAvailableAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return UpdateStateAsync(id, courier => courier.SetAvailable(DateTime.UtcNow), cancellationToken);
    }

    public Task<CourierDto?> SetUnavailableAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return UpdateStateAsync(id, courier => courier.SetUnavailable(DateTime.UtcNow), cancellationToken);
    }

    private async Task<CourierDto?> UpdateStateAsync(
        Guid id,
        Action<Courier> transition,
        CancellationToken cancellationToken)
    {
        var courier = await _courierRepository.GetByIdAsync(id, cancellationToken);
        if (courier is null)
        {
            return null;
        }

        if (!_tenantContext.IsPlatformAdmin
            && (_tenantContext.UserId is not Guid userId || courier.UserId != userId))
        {
            throw new UnauthorizedAccessException(
                "Only the courier can update their own state.");
        }

        transition(courier);
        var updatedCourier = await _courierRepository.SaveStateAsync(courier, cancellationToken);
        return _mapper.Map<CourierDto>(updatedCourier);
    }

    public Task<bool> DeleteAsync(
        Guid id,
        Guid? fleetId = null,
        CancellationToken cancellationToken = default)
    {
        return fleetId.HasValue
            ? _courierRepository.DeleteByIdInFleetAsync(
                id,
                fleetId.Value,
                cancellationToken)
            : _courierRepository.DeleteAsync(id, cancellationToken);
    }
}
