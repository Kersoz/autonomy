namespace Autonomy.Api.Contracts.Deliveries;

/// <summary>Identifies the courier receiving a delivery assignment.</summary>
public sealed record AssignDeliveryRequest
{
    public required Guid CourierId { get; init; }
}
