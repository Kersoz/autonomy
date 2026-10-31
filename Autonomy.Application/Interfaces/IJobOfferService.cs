using Autonomy.Application.DTOs.JobOffers;
using Autonomy.Application.Features.Deliveries;

namespace Autonomy.Application.Interfaces;

public interface IJobOfferService
{
    Task<JobOfferDto?> OfferCourierAsync(
        Guid deliveryId,
        Guid courierId,
        CancellationToken cancellationToken = default);

    Task<JobOfferDto> AcceptAsync(
        Guid offerId,
        CancellationToken cancellationToken = default);

    Task<JobOfferDto> RejectAsync(
        Guid offerId,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<DeliveryDto> CommitAssignmentAsync(
        Guid offerId,
        CancellationToken cancellationToken = default);

    Task<int> ExpirePendingOffersAsync(
        CancellationToken cancellationToken = default);
}
