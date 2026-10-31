using Autonomy.Api.Authorization;
using Autonomy.Api.Contracts.Deliveries;
using Autonomy.Application.Features.Deliveries;
using Autonomy.Application.Features.Dispatching;
using Autonomy.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autonomy.Api.Controllers;

[ApiController]
[Route("api/v1/deliveries")]
[Authorize(Policy = AuthorizationPolicies.DeliveryOperations)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class DeliveriesController(IDeliveryService deliveryService) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType<DeliveryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeliveryResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var delivery = await deliveryService.GetByIdAsync(id, cancellationToken);
        return delivery is null ? NotFound() : Ok(ToResponse(delivery));
    }

    [HttpGet("{id:guid}/candidates")]
    [Authorize(Policy = AuthorizationPolicies.DeliveryDispatch)]
    [ProducesResponseType<IReadOnlyList<DispatchCandidateDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<DispatchCandidateDto>>> FindCandidates(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await deliveryService.FindCandidatesAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.DeliveryManagement)]
    [ProducesResponseType<DeliveryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeliveryResponse>> Create(
        [FromBody] CreateDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        var delivery = await deliveryService.CreateDeliveryAsync(
            new CreateDeliveryCommand(
                request.FleetId,
                request.MerchantId,
                request.CustomerName,
                request.CustomerPhone,
                request.PickupAddress,
                request.PickupLatitude,
                request.PickupLongitude,
                request.DropoffAddress,
                request.DropoffLatitude,
                request.DropoffLongitude,
                request.OrderValue,
                request.PaymentMethod,
                request.Currency),
            cancellationToken);
        var response = ToResponse(delivery);

        return CreatedAtAction(
            nameof(GetById),
            new { id = delivery.Id },
            response);
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Policy = AuthorizationPolicies.DeliveryDispatch)]
    [ProducesResponseType<DeliveryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeliveryResponse>> Assign(
        Guid id,
        [FromBody] AssignDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        var delivery = await deliveryService.AssignAsync(
            id,
            request.CourierId,
            cancellationToken);
        return delivery is null ? NotFound() : Ok(ToResponse(delivery));
    }

    [HttpPost("{id:guid}/reassign")]
    [HttpPost("{id:guid}/force-reassign")]
    [Authorize(Policy = AuthorizationPolicies.DeliveryDispatch)]
    [ProducesResponseType<DeliveryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeliveryResponse>> Reassign(
        Guid id,
        [FromBody] AssignDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        var delivery = await deliveryService.ReassignCourierAsync(
            id,
            request.CourierId,
            cancellationToken);
        return delivery is null ? NotFound() : Ok(ToResponse(delivery));
    }

    [HttpPost("{id:guid}/release-assignment")]
    [Authorize(Policy = AuthorizationPolicies.DeliveryDispatch)]
    [ProducesResponseType<DeliveryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeliveryResponse>> ReleaseAssignment(
        Guid id,
        [FromBody] ReleaseAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var delivery = await deliveryService.ReleaseAssignmentAsync(
            id,
            request.Reason,
            cancellationToken);
        return delivery is null ? NotFound() : Ok(ToResponse(delivery));
    }

    [HttpPost("{id:guid}/pickup")]
    [Authorize(Policy = AuthorizationPolicies.CourierOperations)]
    [ProducesResponseType<DeliveryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeliveryResponse>> Pickup(
        Guid id,
        CancellationToken cancellationToken)
    {
        var delivery = await deliveryService.PickupAsync(id, cancellationToken);
        return delivery is null ? NotFound() : Ok(ToResponse(delivery));
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = AuthorizationPolicies.CourierOperations)]
    [ProducesResponseType<DeliveryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeliveryResponse>> Complete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var delivery = await deliveryService.CompleteDeliveryAsync(id, cancellationToken);
        return delivery is null ? NotFound() : Ok(ToResponse(delivery));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.DeliveryManagement)]
    [ProducesResponseType<DeliveryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeliveryResponse>> Cancel(
        Guid id,
        [FromBody] CancelDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        var delivery = await deliveryService.CancelDeliveryAsync(
            id,
            request.Reason,
            cancellationToken);
        return delivery is null ? NotFound() : Ok(ToResponse(delivery));
    }

    [HttpPost("{id:guid}/fail")]
    [Authorize(Policy = AuthorizationPolicies.CourierOperations)]
    [ProducesResponseType<DeliveryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeliveryResponse>> Fail(
        Guid id,
        CancellationToken cancellationToken)
    {
        var delivery = await deliveryService.FailAsync(id, cancellationToken);
        return delivery is null ? NotFound() : Ok(ToResponse(delivery));
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
