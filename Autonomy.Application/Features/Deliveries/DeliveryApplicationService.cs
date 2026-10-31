using Autonomy.Application.Abstractions.Persistence;
using Autonomy.Application.Interfaces;
using Autonomy.Domain.Entities;

namespace Autonomy.Application.Features.Deliveries;

public sealed class DeliveryApplicationService(
    IDeliveryRepository deliveryRepository,
    ITenantContext tenantContext) : IDeliveryService
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

        if (!await deliveryRepository.CourierBelongsToFleetAsync(
                courierId,
                delivery.FleetId,
                cancellationToken))
        {
            throw new KeyNotFoundException("The courier was not found in the delivery fleet.");
        }

        delivery.Assign(courierId);
        await deliveryRepository.SaveChangesAsync(cancellationToken);
        return ToDto(delivery);
    }

    public Task<DeliveryDto?> PickupAsync(
        Guid deliveryId,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsAssignedCourierAsync(
            deliveryId,
            delivery => delivery.Pickup(),
            cancellationToken);
    }

    public Task<DeliveryDto?> CompleteDeliveryAsync(
        Guid deliveryId,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsAssignedCourierAsync(
            deliveryId,
            delivery => delivery.Deliver(tenantContext.UserId?.ToString()),
            cancellationToken);
    }

    public Task<DeliveryDto?> CancelDeliveryAsync(
        Guid deliveryId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return ChangeDeliveryAsync(
            deliveryId,
            delivery => delivery.Cancel(reason),
            cancellationToken);
    }

    public Task<DeliveryDto?> FailAsync(
        Guid deliveryId,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsAssignedCourierAsync(
            deliveryId,
            delivery => delivery.Fail(),
            cancellationToken);
    }

    private async Task<DeliveryDto?> ChangeAsAssignedCourierAsync(
        Guid deliveryId,
        Action<Delivery> change,
        CancellationToken cancellationToken)
    {
        var delivery = await GetAccessibleDeliveryAsync(deliveryId, cancellationToken);
        if (delivery is null)
        {
            return null;
        }

        if (tenantContext.UserId is not Guid userId
            || !await deliveryRepository.IsAssignedCourierAsync(
                deliveryId,
                userId,
                cancellationToken))
        {
            throw new UnauthorizedAccessException(
                "Only the courier assigned to this delivery can update its status.");
        }

        change(delivery);
        await deliveryRepository.SaveChangesAsync(cancellationToken);
        return ToDto(delivery);
    }

    private async Task<DeliveryDto?> ChangeDeliveryAsync(
        Guid deliveryId,
        Action<Delivery> change,
        CancellationToken cancellationToken)
    {
        var delivery = await GetAccessibleDeliveryAsync(deliveryId, cancellationToken);
        if (delivery is null)
        {
            return null;
        }

        change(delivery);
        await deliveryRepository.SaveChangesAsync(cancellationToken);
        return ToDto(delivery);
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
