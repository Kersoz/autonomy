using Autonomy.Api.Authorization;
using Autonomy.Application.DTOs.Couriers;
using Autonomy.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autonomy.Api.Controllers;

[ApiController]
[Route("api/v1/couriers/{id:guid}")]
[Authorize(Policy = AuthorizationPolicies.CourierOperations)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class CourierStateController(ICourierService courierService) : ControllerBase
{
    [HttpPost("go-online")]
    [ProducesResponseType<CourierDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourierDto>> GoOnline(
        Guid id,
        CancellationToken cancellationToken)
    {
        var courier = await courierService.GoOnlineAsync(id, cancellationToken);
        return courier is null ? NotFound() : Ok(courier);
    }

    [HttpPost("go-offline")]
    [ProducesResponseType<CourierDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourierDto>> GoOffline(
        Guid id,
        CancellationToken cancellationToken)
    {
        var courier = await courierService.GoOfflineAsync(id, cancellationToken);
        return courier is null ? NotFound() : Ok(courier);
    }

    [HttpPost("set-available")]
    [ProducesResponseType<CourierDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourierDto>> SetAvailable(
        Guid id,
        CancellationToken cancellationToken)
    {
        var courier = await courierService.SetAvailableAsync(id, cancellationToken);
        return courier is null ? NotFound() : Ok(courier);
    }

    [HttpPost("set-unavailable")]
    [ProducesResponseType<CourierDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourierDto>> SetUnavailable(
        Guid id,
        CancellationToken cancellationToken)
    {
        var courier = await courierService.SetUnavailableAsync(id, cancellationToken);
        return courier is null ? NotFound() : Ok(courier);
    }
}
