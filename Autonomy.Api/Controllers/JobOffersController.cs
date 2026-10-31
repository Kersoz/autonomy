using Autonomy.Api.Authorization;
using Autonomy.Api.Contracts.Deliveries;
using Autonomy.Api.Contracts.JobOffers;
using Autonomy.Application.DTOs.JobOffers;
using Autonomy.Application.Features.Deliveries;
using Autonomy.Application.Interfaces;
using Autonomy.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autonomy.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class JobOffersController(IJobOfferService jobOfferService) : ControllerBase
{
    [HttpPost("deliveries/{deliveryId:guid}/offers")]
    [Authorize(Policy = AuthorizationPolicies.DeliveryDispatch)]
    [ProducesResponseType<JobOfferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobOfferResponse>> OfferCourier(
        Guid deliveryId,
        [FromBody] OfferCourierRequest request,
        CancellationToken cancellationToken)
    {
        var offer = await jobOfferService.OfferCourierAsync(
            deliveryId,
            request.CourierId,
            cancellationToken);
        return offer is null ? NotFound() : Ok(ToResponse(offer));
    }

    [HttpPost("job-offers/{offerId:guid}/accept")]
    [Authorize(Policy = AuthorizationPolicies.CourierOperations)]
    [ProducesResponseType<JobOfferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<JobOfferResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobOfferResponse>> Accept(
        Guid offerId,
        CancellationToken cancellationToken)
    {
        var offer = await jobOfferService.AcceptAsync(offerId, cancellationToken);
        var response = ToResponse(offer);
        return offer.Status == OfferStatus.Expired ? Conflict(response) : Ok(response);
    }

    [HttpPost("job-offers/{offerId:guid}/reject")]
    [Authorize(Policy = AuthorizationPolicies.CourierOperations)]
    [ProducesResponseType<JobOfferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<JobOfferResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobOfferResponse>> Reject(
        Guid offerId,
        [FromBody] RejectJobOfferRequest request,
        CancellationToken cancellationToken)
    {
        var offer = await jobOfferService.RejectAsync(
            offerId,
            request.Reason,
            cancellationToken);
        var response = ToResponse(offer);
        return offer.Status == OfferStatus.Expired ? Conflict(response) : Ok(response);
    }

    [HttpPost("job-offers/{offerId:guid}/commit-assignment")]
    [Authorize(Policy = AuthorizationPolicies.DeliveryDispatch)]
    [ProducesResponseType<DeliveryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeliveryResponse>> CommitAssignment(
        Guid offerId,
        CancellationToken cancellationToken)
    {
        var delivery = await jobOfferService.CommitAssignmentAsync(
            offerId,
            cancellationToken);
        return Ok(ToResponse(delivery));
    }

    private static JobOfferResponse ToResponse(JobOfferDto offer)
    {
        return new JobOfferResponse(
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

    private static DeliveryResponse ToResponse(DeliveryDto delivery)
    {
        return new DeliveryResponse(
            delivery.Id,
            delivery.ReferenceNumber,
            delivery.FleetId,
            delivery.MerchantId,
            delivery.Status,
            delivery.AssignedCourierId,
            delivery.CreatedAt,
            delivery.UpdatedAt);
    }
}
