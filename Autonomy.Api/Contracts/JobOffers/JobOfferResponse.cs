using Autonomy.Domain.Enums;

namespace Autonomy.Api.Contracts.JobOffers;

public sealed record JobOfferResponse(
    Guid Id,
    Guid DeliveryId,
    Guid CourierId,
    OfferStatus Status,
    DateTime OfferedAt,
    DateTime ExpiresAt,
    DateTime? RespondedAt,
    string? RejectionReason);
