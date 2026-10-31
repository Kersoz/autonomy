using Autonomy.Application.Abstractions.Persistence;
using Autonomy.Application.DTOs.JobOffers;
using Autonomy.Application.Features.Deliveries;
using Autonomy.Application.Features.Dispatching;
using Autonomy.Application.Interfaces;
using Autonomy.Domain.Entities;
using Autonomy.Domain.Enums;
using Autonomy.Domain.Exceptions;

namespace Autonomy.Application.Features.JobOffers;

public sealed class JobOfferApplicationService(
    IJobOfferRepository jobOfferRepository,
    ITenantContext tenantContext) : IJobOfferService
{
    private static readonly TimeSpan OfferLifetime = TimeSpan.FromMinutes(5);

    public async Task<JobOfferDto?> OfferCourierAsync(
        Guid deliveryId,
        Guid courierId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var offer = await jobOfferRepository.ExecuteAtomicallyAsync(
            null,
            deliveryId,
            courierId,
            transaction =>
            {
                var delivery = transaction.Delivery;
                if (delivery is null)
                {
                    return null;
                }

                var courier = transaction.Courier
                    ?? throw new KeyNotFoundException(
                        "The courier was not found in the delivery fleet.");
                if (courier.FleetId != delivery.FleetId)
                {
                    throw new KeyNotFoundException(
                        "The courier was not found in the delivery fleet.");
                }

                ValidateCourierState(courier);
                var location = transaction.LatestLocation;
                if (location is null
                    || !DispatchCandidateRanking.IsFreshAndValidLocation(location, now))
                {
                    throw new AssignmentConflictException(
                        "The courier does not have a fresh, valid location.");
                }

                if (courier.ActiveDeliveryCount >= courier.MaxSimultaneousDeliveries)
                {
                    throw new AssignmentConflictException(
                        "The courier has reached concurrent assignment capacity.");
                }

                var (distanceKm, etaMinutes) = DispatchCandidateRanking.Calculate(
                    location.Latitude,
                    location.Longitude,
                    delivery.PickupLatitude,
                    delivery.PickupLongitude);

                if (delivery.Status != DeliveryStatus.PendingAssignment
                    || transaction.ActiveAssignment is not null)
                {
                    throw new AssignmentConflictException(
                        "Only unassigned deliveries can be offered.");
                }

                var createdOffer = new JobOffer
                {
                    Id = Guid.NewGuid(),
                    FleetId = delivery.FleetId,
                    Delivery = delivery,
                    DeliveryId = delivery.Id,
                    Courier = courier,
                    CourierId = courier.Id,
                    OfferedAt = now,
                    ExpiresAt = now.Add(OfferLifetime),
                    Status = OfferStatus.Pending,
                    EstimatedDistanceKm = distanceKm,
                    EstimatedTravelTimeMinutes = etaMinutes
                };
                return createdOffer;
            },
            cancellationToken);

        return offer is null ? null : ToDto(offer);
    }

    public Task<JobOfferDto> AcceptAsync(
        Guid offerId,
        CancellationToken cancellationToken = default)
    {
        return RespondAsync(
            offerId,
            (offer, now) =>
            {
                if (offer.ExpiresAt <= now)
                {
                    offer.Expire(now);
                    return;
                }

                offer.Accept(now);
            },
            cancellationToken);
    }

    public Task<JobOfferDto> RejectAsync(
        Guid offerId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        if (reason?.Length > 500)
        {
            throw new ArgumentException("The rejection reason cannot exceed 500 characters.");
        }

        return RespondAsync(
            offerId,
            (offer, now) =>
            {
                if (offer.ExpiresAt <= now)
                {
                    offer.Expire(now);
                    return;
                }

                offer.Reject(now, reason);
            },
            cancellationToken);
    }

    public async Task<DeliveryDto> CommitAssignmentAsync(
        Guid offerId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await jobOfferRepository.ExecuteAtomicallyAsync(
            offerId,
            null,
            null,
            transaction =>
            {
                var offer = transaction.Offer
                    ?? throw new KeyNotFoundException("The job offer was not found.");

                if (offer.Status != OfferStatus.Accepted)
                {
                    throw new AssignmentConflictException(
                        "Only accepted offers can be committed as assignments.");
                }

                var delivery = transaction.Delivery
                    ?? throw new KeyNotFoundException("The delivery was not found.");
                var courier = transaction.Courier
                    ?? throw new KeyNotFoundException("The courier was not found.");
                if (delivery.Status != DeliveryStatus.PendingAssignment
                    || transaction.ActiveAssignment is not null)
                {
                    throw new AssignmentConflictException(
                        "The delivery is no longer available for assignment.");
                }

                ValidateCourier(delivery, courier, transaction.LatestLocation, DateTime.UtcNow);
                if (courier.ActiveDeliveryCount >= courier.MaxSimultaneousDeliveries)
                {
                    throw new AssignmentConflictException(
                        "The courier has reached concurrent assignment capacity.");
                }

                delivery.Assign(courier.Id);
                var createdAssignment = new Assignment
                {
                    Id = Guid.NewGuid(),
                    FleetId = delivery.FleetId,
                    Delivery = delivery,
                    DeliveryId = delivery.Id,
                    Courier = courier,
                    CourierId = courier.Id,
                    AssignedAt = DateTime.UtcNow,
                    Source = AssignmentSource.AcceptedOffer,
                    Status = AssignmentStatus.Active,
                    RelatedOffer = offer,
                    RelatedOfferId = offer.Id,
                    DispatchOrigin = DispatchOrigin.Automatic,
                    SelectedBy = courier.UserId.ToString()
                };
                delivery.ActiveAssignment = createdAssignment;
                delivery.ActiveAssignmentId = createdAssignment.Id;
                return delivery;
            },
            cancellationToken);

        return ToDeliveryDto(assignment);
    }

    public Task<int> ExpirePendingOffersAsync(
        CancellationToken cancellationToken = default)
    {
        return jobOfferRepository.ExpirePendingOffersAsync(
            DateTime.UtcNow,
            cancellationToken);
    }

    private async Task<JobOfferDto> RespondAsync(
        Guid offerId,
        Action<JobOffer, DateTime> respond,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var offer = await jobOfferRepository.ExecuteAtomicallyAsync(
            offerId,
            null,
            null,
            transaction =>
            {
                var existingOffer = transaction.Offer
                    ?? throw new KeyNotFoundException("The job offer was not found.");
                EnsureCourierActor(transaction.Courier);
                respond(existingOffer, now);
                return existingOffer;
            },
            cancellationToken);

        return ToDto(offer);
    }

    private void EnsureCourierActor(Courier? courier)
    {
        if (courier is null)
        {
            throw new KeyNotFoundException("The courier was not found.");
        }

        if (!tenantContext.IsPlatformAdmin
            && (tenantContext.UserId is not Guid userId || courier.UserId != userId))
        {
            throw new UnauthorizedAccessException(
                "Only the courier receiving this offer can respond to it.");
        }
    }

    private static void ValidateCourier(
        Delivery delivery,
        Courier courier,
        CourierLocation? latestLocation,
        DateTime now)
    {
        if (courier.FleetId != delivery.FleetId)
        {
            throw new AssignmentConflictException(
                "The courier does not belong to the delivery fleet.");
        }

        ValidateCourierState(courier);
        if (!DispatchCandidateRanking.IsFreshAndValidLocation(latestLocation, now))
        {
            throw new AssignmentConflictException(
                "The courier does not have a fresh, valid location.");
        }
    }

    private static void ValidateCourierState(Courier courier)
    {
        if (!courier.IsEnabled)
        {
            throw new AssignmentConflictException("The courier is disabled.");
        }

        if (courier.OnlineStatus != CourierOnlineStatus.Online)
        {
            throw new AssignmentConflictException("The courier is offline.");
        }

        if (courier.AvailabilityStatus != CourierAvailabilityStatus.Available)
        {
            throw new AssignmentConflictException("The courier is unavailable.");
        }
    }

    private static JobOfferDto ToDto(JobOffer offer)
    {
        return new JobOfferDto(
            offer.Id,
            offer.DeliveryId,
            offer.CourierId,
            offer.Status,
            offer.OfferedAt,
            offer.ExpiresAt,
            offer.RespondedAt,
            offer.RejectionReason,
            offer.EstimatedDistanceKm,
            offer.EstimatedTravelTimeMinutes);
    }

    private static DeliveryDto ToDeliveryDto(Delivery delivery)
    {
        return new DeliveryDto(
            delivery.Id,
            delivery.ReferenceNumber,
            delivery.FleetId,
            delivery.MerchantId,
            delivery.Status,
            delivery.AssignedCourierId,
            new DateTimeOffset(DateTime.SpecifyKind(delivery.CreatedAt, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(delivery.UpdatedAt, DateTimeKind.Utc)));
    }
}
