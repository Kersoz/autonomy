namespace Autonomy.Api.Contracts.Deliveries;

/// <summary>Provides the reason for releasing an assignment.</summary>
public sealed record ReleaseAssignmentRequest
{
    public required string Reason { get; init; }
}
