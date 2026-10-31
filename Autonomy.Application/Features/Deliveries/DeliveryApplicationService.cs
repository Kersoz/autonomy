using Autonomy.Application.Abstractions.Persistence;
using Autonomy.Application.Features.Dispatching;
using Autonomy.Application.Interfaces;
using Autonomy.Domain.Entities;
using Autonomy.Domain.Enums;
using Autonomy.Domain.Exceptions;

namespace Autonomy.Application.Features.Deliveries;

public sealed class DeliveryApplicationService(
    IDeliveryRepository deliveryRepository,
    ICourierRepository courierRepository,
    ITenantContext tenantContext,
    IAssignmentService assignmentService) : IDeliveryService
{
    public async Task<DeliveryDto> CreateDeliveryAsync(
        CreateDeliveryCommand command,
        CancellationToken cancellationToken = default)
    {
        EnsureFleetAccess(command.FleetId);
        EnsureMerchantAccess(command.MerchantId);

        if (!await deliveryRepository.MerchantBelongsToFleetAsync(
                command.MerchantId,
                command.FleetId,
                cancellationToken))
        {
            throw new KeyNotFoundException("The merchant was not found in the specified fleet.");
        }

        var delivery = new Delivery
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = $"DLV-{Guid.NewGuid():N}",
            FleetId = command.FleetId,
            MerchantId = command.MerchantId,
            CustomerName = command.CustomerName.Trim(),
            CustomerPhone = command.CustomerPhone.Trim(),
            PickupAddress = command.PickupAddress.Trim(),
            PickupLatitude = command.PickupLatitude,
            PickupLongitude = command.PickupLongitude,
            DropoffAddress = command.DropoffAddress.Trim(),
            DropoffLatitude = command.DropoffLatitude,
            DropoffLongitude = command.DropoffLongitude,
            OrderValue = command.OrderValue,
            PaymentMethod = command.PaymentMethod,
            Currency = command.Currency.Trim().ToUpperInvariant(),
            CreatedByUserId = tenantContext.UserId
        };
        delivery.QueueForAssignment();

        deliveryRepository.Add(delivery);
        await deliveryRepository.SaveChangesAsync(cancellationToken);
        return ToDto(delivery);
    }

    public async Task<DeliveryDto?> GetByIdAsync(
        Guid deliveryId,
        CancellationToken cancellationToken = default)
    {
        var delivery = await GetAccessibleDeliveryAsync(deliveryId, cancellationToken);
        return delivery is null ? null : ToDto(delivery);
    }

    public async Task<IReadOnlyList<DispatchCandidateDto>> FindCandidatesAsync(
        Guid deliveryId,
        CancellationToken cancellationToken = default)
    {
        var delivery = await GetAccessibleDeliveryAsync(deliveryId, cancellationToken);
        if (delivery is null)
        {
            throw new KeyNotFoundException("The delivery was not found.");
        }

        if (delivery.Status != DeliveryStatus.PendingAssignment)
        {
            throw new AssignmentConflictException(
                "Candidates can only be found for unassigned deliveries.");
        }

        var now = DateTime.UtcNow;
        return (await courierRepository.GetDispatchCandidatesAsync(
                delivery.FleetId,
                cancellationToken))
            .Where(candidate => DispatchCandidateRanking.IsFreshAndValidLocation(
                candidate.LatestLocation,
                now))
            .Select(candidate =>
            {
                var location = candidate.LatestLocation!;
                var (distanceKm, etaMinutes) = DispatchCandidateRanking.Calculate(
                    location.Latitude,
                    location.Longitude,
                    delivery.PickupLatitude,
                    delivery.PickupLongitude);
                return new DispatchCandidateDto(
                    candidate.Courier.Id,
                    candidate.Courier.Name,
                    distanceKm,
                    etaMinutes,
                    location.RecordedAt);
            })
            .OrderBy(candidate => candidate.DistanceKm)
            .ThenBy(candidate => candidate.EstimatedArrivalMinutes)
            .ThenBy(candidate => candidate.CourierName)
            .ThenBy(candidate => candidate.CourierId)
            .ToArray();
    }

    public async Task<DeliveryDto?> AssignAsync(
        Guid deliveryId,
        Guid courierId,
        CancellationToken cancellationToken = default)
    {
        var delivery = await GetAccessibleDeliveryAsync(deliveryId, cancellationToken);
        if (delivery is null)
        {
            return null;
        }

        return await assignmentService.AssignCourierAsync(
            deliveryId,
            courierId,
            cancellationToken);
    }

    public async Task<DeliveryDto?> ReleaseAssignmentAsync(
        Guid deliveryId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (await GetAccessibleDeliveryAsync(deliveryId, cancellationToken) is null)
        {
            return null;
        }

        return await assignmentService.ReleaseAssignmentAsync(
            deliveryId,
            reason,
            cancellationToken);
    }

    public async Task<DeliveryDto?> ReassignCourierAsync(
        Guid deliveryId,
        Guid courierId,
        CancellationToken cancellationToken = default)
    {
        if (await GetAccessibleDeliveryAsync(deliveryId, cancellationToken) is null)
        {
            return null;
        }

        return await assignmentService.ReassignCourierAsync(
            deliveryId,
            courierId,
            cancellationToken);
    }

    public async Task<DeliveryDto?> PickupAsync(
        Guid deliveryId,
        CancellationToken cancellationToken = default)
    {
        if (await GetAccessibleDeliveryAsync(deliveryId, cancellationToken) is null)
        {
            return null;
        }

        return await assignmentService.PickupAsync(
            deliveryId,
            tenantContext.UserId,
            cancellationToken);
    }

    public async Task<DeliveryDto?> CompleteDeliveryAsync(
        Guid deliveryId,
        CancellationToken cancellationToken = default)
    {
        if (await GetAccessibleDeliveryAsync(deliveryId, cancellationToken) is null)
        {
            return null;
        }

        return await assignmentService.CompleteDeliveryAsync(
            deliveryId,
            tenantContext.UserId,
            cancellationToken);
    }

    public async Task<DeliveryDto?> CancelDeliveryAsync(
        Guid deliveryId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (await GetAccessibleDeliveryAsync(deliveryId, cancellationToken) is null)
        {
            return null;
        }

        return await assignmentService.CancelDeliveryAsync(
            deliveryId,
            reason,
            cancellationToken);
    }

    public async Task<DeliveryDto?> FailAsync(
        Guid deliveryId,
        CancellationToken cancellationToken = default)
    {
        if (await GetAccessibleDeliveryAsync(deliveryId, cancellationToken) is null)
        {
            return null;
        }

        return await assignmentService.FailAsync(
            deliveryId,
            tenantContext.UserId,
            cancellationToken);
    }

    private async Task<Delivery?> GetAccessibleDeliveryAsync(
        Guid deliveryId,
        CancellationToken cancellationToken)
    {
        var delivery = await deliveryRepository.GetByIdAsync(
            deliveryId,
            cancellationToken);
        if (delivery is not null)
        {
            EnsureMerchantAccess(delivery.MerchantId);
        }

        return delivery;
    }

    private void EnsureFleetAccess(Guid fleetId)
    {
        if (tenantContext.IsPlatformAdmin)
        {
            return;
        }

        if (tenantContext.TenantId is not Guid currentTenantId
            || currentTenantId != fleetId)
        {
            throw new UnauthorizedAccessException(
                "The active tenant cannot access this fleet.");
        }
    }

    private void EnsureMerchantAccess(Guid merchantId)
    {
        if (tenantContext.MerchantId is Guid currentMerchantId
            && currentMerchantId != merchantId)
        {
            throw new UnauthorizedAccessException(
                "The active merchant cannot access this delivery.");
        }
    }

    private static DeliveryDto ToDto(Delivery delivery)
    {
        return new DeliveryDto(
            delivery.Id,
            delivery.ReferenceNumber,
            delivery.FleetId,
            delivery.MerchantId,
            delivery.Status,
            delivery.AssignedCourierId,
            new DateTimeOffset(DateTime.SpecifyKind(delivery.CreatedAt, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(delivery.UpdatedAt, DateTimeKind.Utc)));
    }
}
