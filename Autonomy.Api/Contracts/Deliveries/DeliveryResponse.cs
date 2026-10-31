using Autonomy.Domain.Enums;

namespace Autonomy.Api.Contracts.Deliveries;

/// <summary>Represents a delivery and its current lifecycle state.</summary>
public sealed record DeliveryResponse(
    Guid Id,
    string ReferenceNumber,
    Guid FleetId,
    Guid MerchantId,
    DeliveryStatus Status,
    Guid? AssignedCourierId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
