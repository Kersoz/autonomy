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

    public CourierService(ICourierRepository courierRepository, IMapper mapper)
    {
        _courierRepository = courierRepository;
        _mapper = mapper;
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
