namespace Autonomy.Application.Features.Dispatching;

public sealed record DispatchCandidateDto(
    Guid CourierId,
    string CourierName,
    decimal DistanceKm,
    int EstimatedArrivalMinutes,
    DateTime LocationRecordedAt);
