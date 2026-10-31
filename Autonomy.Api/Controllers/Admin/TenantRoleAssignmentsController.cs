using Autonomy.Api.Authorization;
using Autonomy.Api.Contracts.Auth;
using Autonomy.Persistence.Context;
using Autonomy.Persistence.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Autonomy.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/tenant-role-assignments")]
[Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class TenantRoleAssignmentsController(
    AutonomyDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType<TenantRoleAssignmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantRoleAssignmentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var assignment = await dbContext.TenantRoleAssignments
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return assignment is null
            ? NotFound()
            : Ok(ToResponse(assignment));
    }

    [HttpPost]
    [ProducesResponseType<TenantRoleAssignmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TenantRoleAssignmentResponse>> Create(
        [FromBody] CreateTenantRoleAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!AuthorizationPolicies.IsRoleAllowedForTenant(request.RoleName)
            || (request.RoleName == AuthorizationRoles.MerchantUser)
                != request.MerchantId.HasValue)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid tenant role",
                Detail = "MerchantUser requires a merchant; fleet roles must not include one."
            });
        }

        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid user",
                Detail = "The user does not exist or is inactive."
            });
        }

        if (!await dbContext.Fleets.AnyAsync(
                fleet => fleet.Id == request.TenantId,
                cancellationToken))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid tenant",
                Detail = "The specified tenant does not exist."
            });
        }

        if (request.MerchantId.HasValue
            && !await dbContext.Merchants.AnyAsync(
                merchant => merchant.Id == request.MerchantId.Value
                    && merchant.FleetId == request.TenantId,
                cancellationToken))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid merchant",
                Detail = "The merchant does not belong to the specified fleet."
            });
        }

        var alreadyAssigned = await dbContext.TenantRoleAssignments
            .AnyAsync(assignment =>
                assignment.UserId == request.UserId
                && assignment.TenantId == request.TenantId
                && assignment.MerchantId == request.MerchantId
                && assignment.RoleName == request.RoleName,
                cancellationToken);
        if (alreadyAssigned)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Role already assigned",
                Detail = "This user already has the specified role in this tenant."
            });
        }

        var assignment = new TenantRoleAssignment
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            TenantId = request.TenantId,
            MerchantId = request.MerchantId,
            RoleName = request.RoleName,
            IsActive = true
        };
        dbContext.TenantRoleAssignments.Add(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = ToResponse(assignment);
        return CreatedAtAction(
            nameof(GetById),
            new { id = assignment.Id },
            response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var assignment = await dbContext.TenantRoleAssignments
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (assignment is null)
        {
            return NotFound();
        }

        dbContext.TenantRoleAssignments.Remove(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static TenantRoleAssignmentResponse ToResponse(
        TenantRoleAssignment assignment)
    {
        return new TenantRoleAssignmentResponse(
            assignment.Id,
            assignment.UserId,
            assignment.TenantId,
            assignment.MerchantId,
            assignment.RoleName);
    }
}
