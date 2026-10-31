namespace Autonomy.Api.Contracts.JobOffers;

public sealed record OfferCourierRequest
{
    public required Guid CourierId { get; init; }
}
