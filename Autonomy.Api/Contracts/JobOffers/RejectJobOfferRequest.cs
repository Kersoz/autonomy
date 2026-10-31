namespace Autonomy.Api.Contracts.JobOffers;

public sealed record RejectJobOfferRequest
{
    public string? Reason { get; init; }
}
