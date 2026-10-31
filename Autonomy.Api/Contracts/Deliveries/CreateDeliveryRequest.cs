using Autonomy.Domain.Enums;

namespace Autonomy.Api.Contracts.Deliveries;

/// <summary>Information required to create a delivery.</summary>
public sealed record CreateDeliveryRequest
{
    public required Guid FleetId { get; init; }

    public required Guid MerchantId { get; init; }

    public required string CustomerName { get; init; }

    public required string CustomerPhone { get; init; }

    public required string PickupAddress { get; init; }

    public required decimal PickupLatitude { get; init; }

    public required decimal PickupLongitude { get; init; }

    public required string DropoffAddress { get; init; }

    public required decimal DropoffLatitude { get; init; }

    public required decimal DropoffLongitude { get; init; }

    public required decimal OrderValue { get; init; }

    public required PaymentMethod PaymentMethod { get; init; }

    public string Currency { get; init; } = "TRY";
}
