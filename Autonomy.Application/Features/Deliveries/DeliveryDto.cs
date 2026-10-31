using Autonomy.Domain.Enums;

namespace Autonomy.Application.Features.Deliveries;

public sealed record DeliveryDto(
    Guid Id,
    string ReferenceNumber,
    Guid FleetId,
    Guid MerchantId,
    DeliveryStatus Status,
    Guid? AssignedCourierId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
