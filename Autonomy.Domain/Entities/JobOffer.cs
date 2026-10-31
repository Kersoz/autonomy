using Autonomy.Domain.Common;
using Autonomy.Domain.Enums;
using Autonomy.Domain.Exceptions;

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

    public void Accept(DateTime respondedAt)
    {
        EnsurePending();
        Status = OfferStatus.Accepted;
        RespondedAt = respondedAt;
    }

    public void Reject(DateTime respondedAt, string? reason)
    {
        EnsurePending();
        Status = OfferStatus.Rejected;
        RespondedAt = respondedAt;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public void Expire(DateTime respondedAt)
    {
        EnsurePending();
        Status = OfferStatus.Expired;
        RespondedAt = respondedAt;
    }

    private void EnsurePending()
    {
        if (Status != OfferStatus.Pending)
        {
            throw new AssignmentConflictException(
                "Only pending offers can be changed.");
        }
    }
}