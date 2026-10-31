using Autonomy.Domain.Enums;

namespace Autonomy.Application.Features.Deliveries;

public sealed record CreateDeliveryCommand(
    Guid FleetId,
    Guid MerchantId,
    string CustomerName,
    string CustomerPhone,
    string PickupAddress,
    decimal PickupLatitude,
    decimal PickupLongitude,
    string DropoffAddress,
    decimal DropoffLatitude,
    decimal DropoffLongitude,
    decimal OrderValue,
    PaymentMethod PaymentMethod,
    string Currency);
