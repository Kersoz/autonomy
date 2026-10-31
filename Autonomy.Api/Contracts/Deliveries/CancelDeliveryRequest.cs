namespace Autonomy.Api.Contracts.Deliveries;

/// <summary>Provides the reason for cancelling a delivery.</summary>
public sealed record CancelDeliveryRequest
{
    public required string Reason { get; init; }
}
