using Autonomy.Domain.Enums;

namespace Autonomy.Application.DTOs.JobOffers;

public sealed record JobOfferDto(
    Guid Id,
    Guid DeliveryId,
    Guid CourierId,
    OfferStatus Status,
    DateTime OfferedAt,
    DateTime ExpiresAt,
    DateTime? RespondedAt,
    string? RejectionReason);
