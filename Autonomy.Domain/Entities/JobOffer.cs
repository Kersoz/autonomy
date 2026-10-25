using Autonomy.Domain.Common;
using Autonomy.Domain.Enums;

namespace Autonomy.Domain.Entities;

public class JobOffer : BaseEntity
{
    public Guid FleetId { get; set; }

    public Fleet Fleet { get; set; } = null!;

    public Guid DeliveryId { get; set; }

    public Delivery Delivery { get; set; } = null!;

    public Guid CourierId { get; set; }

    public Courier Courier { get; set; } = null!;

    public DateTime OfferedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public OfferStatus Status { get; set; }

    public DateTime? RespondedAt { get; set; }

    public string? RejectionReason { get; set; }

    public decimal? EstimatedDistanceKm { get; set; }

    public int? EstimatedTravelTimeMinutes { get; set; }
}