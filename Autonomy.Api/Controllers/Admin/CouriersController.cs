using Autonomy.Application.DTOs.Couriers;
using Autonomy.Application.Interfaces;
using Autonomy.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autonomy.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/couriers")]
[Authorize(Policy = AuthorizationPolicies.FleetAdmin)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class CouriersController : ControllerBase
{
    private readonly ICourierService _courierService;

    public CouriersController(ICourierService courierService)
    {
        _courierService = courierService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CourierDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourierDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var couriers = await _courierService.GetAllAsync(GetFleetId(), cancellationToken);
        return Ok(couriers);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CourierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourierDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var courier = await _courierService.GetByIdAsync(
            id,
            GetFleetId(),
            cancellationToken);
        return courier is null ? NotFound() : Ok(courier);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CourierDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CourierDto>> Create(
        [FromBody] CreateCourierDto courier,
        CancellationToken cancellationToken)
    {
        var fleetId = GetFleetId();
        if (fleetId.HasValue && courier.FleetId != fleetId.Value)
        {
            return Forbid();
        }

        var createdCourier = await _courierService.CreateAsync(courier, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdCourier.Id },
            createdCourier);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CourierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CourierDto>> Update(
        Guid id,
        [FromBody] UpdateCourierDto courier,
        CancellationToken cancellationToken)
    {
        var updatedCourier = await _courierService.UpdateAsync(
            id,
            courier,
            GetFleetId(),
            cancellationToken);
        return updatedCourier is null ? NotFound() : Ok(updatedCourier);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _courierService.DeleteAsync(
            id,
            GetFleetId(),
            cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private Guid? GetFleetId()
    {
        if (AuthorizationPolicies.IsPlatformAdmin(User))
        {
            return null;
        }

        return Guid.TryParse(
            User.FindFirst(AuthorizationPolicies.TenantIdClaim)?.Value,
            out var fleetId)
            ? fleetId
            : throw new InvalidOperationException("Authorized fleet user has no valid fleet claim.");
    }
}
