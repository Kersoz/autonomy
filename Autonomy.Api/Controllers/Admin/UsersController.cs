using System.Security.Claims;
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
[Route("api/v1/admin/users")]
[Authorize(Policy = AuthorizationPolicies.UserProvisioning)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class UsersController(
    AutonomyDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailsResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var roles = new List<UserRoleAssignmentResponse>();
        if (AuthorizationPolicies.IsPlatformAdmin(User)
            && await userManager.IsInRoleAsync(user, AuthorizationRoles.PlatformAdmin))
        {
            roles.Add(new UserRoleAssignmentResponse(
                AuthorizationRoles.PlatformAdmin,
                null,
                null));
        }

        var assignmentsQuery = dbContext.TenantRoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == id);
        if (!AuthorizationPolicies.IsPlatformAdmin(User))
        {
            if (!Guid.TryParse(
                    User.FindFirst(AuthorizationPolicies.TenantIdClaim)?.Value,
                    out var fleetId))
            {
                return Forbid();
            }

            assignmentsQuery = assignmentsQuery.Where(assignment =>
                assignment.TenantId == fleetId);
        }

        var tenantRoles = await assignmentsQuery
            .Select(assignment => new UserRoleAssignmentResponse(
                assignment.RoleName,
                assignment.TenantId,
                assignment.MerchantId))
            .ToListAsync(cancellationToken);
        roles.AddRange(tenantRoles);

        if (roles.Count == 0)
        {
            return NotFound();
        }

        return Ok(new UserDetailsResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.IsActive,
            roles));
    }

    [HttpPost]
    [ProducesResponseType<CreatedUserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedUserResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var scopeResult = await ResolveAssignmentScopeAsync(request, cancellationToken);
        if (scopeResult.Error is not null)
        {
            return scopeResult.Error;
        }

        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Email already registered",
                Detail = "An account with this email address already exists."
            });
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return IdentityErrors(createResult);
        }

        if (request.RoleName == AuthorizationRoles.PlatformAdmin)
        {
            var roleResult = await userManager.AddToRoleAsync(
                user,
                AuthorizationRoles.PlatformAdmin);
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return IdentityErrors(roleResult);
            }
        }
        else
        {
            dbContext.TenantRoleAssignments.Add(new TenantRoleAssignment
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TenantId = scopeResult.TenantId!.Value,
                MerchantId = scopeResult.MerchantId,
                RoleName = request.RoleName,
                IsActive = true
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        var response = new CreatedUserResponse(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            request.RoleName,
            scopeResult.TenantId,
            scopeResult.MerchantId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            response);
    }

    private async Task<AssignmentScopeResult> ResolveAssignmentScopeAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (AuthorizationPolicies.IsPlatformAdmin(User))
        {
            if (request.RoleName == AuthorizationRoles.PlatformAdmin)
            {
                return request.TenantId is null && request.MerchantId is null
                    ? AssignmentScopeResult.Global
                    : AssignmentScopeResult.Invalid(
                        "PlatformAdmin is a global role and must not include tenant fields.");
            }

            if (!AuthorizationPolicies.IsRoleAllowedForTenant(request.RoleName)
                || !request.TenantId.HasValue
                || (request.RoleName == AuthorizationRoles.MerchantUser)
                    != request.MerchantId.HasValue)
            {
                return AssignmentScopeResult.Invalid(
                    "Provide a fleet tenant; MerchantUser requires a merchant ID.");
            }

            if (!await dbContext.Fleets.AnyAsync(
                    fleet => fleet.Id == request.TenantId.Value,
                    cancellationToken))
            {
                return AssignmentScopeResult.Invalid("The specified fleet does not exist.");
            }

            if (request.MerchantId.HasValue
                && !await dbContext.Merchants.AnyAsync(
                    merchant => merchant.Id == request.MerchantId.Value
                        && merchant.FleetId == request.TenantId.Value,
                    cancellationToken))
            {
                return AssignmentScopeResult.Invalid(
                    "The merchant does not belong to the specified fleet.");
            }

            return AssignmentScopeResult.ForTenant(
                request.TenantId.Value,
                request.MerchantId);
        }

        if (!AuthorizationPolicies.IsFleetAdmin(User))
        {
            return AssignmentScopeResult.Invalid("The caller cannot create users.");
        }

        if (request.RoleName is not (AuthorizationRoles.Dispatcher or AuthorizationRoles.Courier))
        {
            return AssignmentScopeResult.Invalid(
                "FleetAdmin can create only Dispatcher or Courier accounts.");
        }

        if (!Guid.TryParse(
                User.FindFirst(AuthorizationPolicies.TenantIdClaim)?.Value,
                out var callerFleetId))
        {
            return AssignmentScopeResult.Invalid("The caller has no valid fleet context.");
        }

        if (request.TenantId.HasValue && request.TenantId.Value != callerFleetId
            || request.MerchantId.HasValue)
        {
            return AssignmentScopeResult.Invalid(
                "FleetAdmin can create users only in the active fleet.");
        }

        return AssignmentScopeResult.ForTenant(callerFleetId, null);
    }

    private static BadRequestObjectResult IdentityErrors(IdentityResult result)
    {
        var errors = result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());
        var isDuplicate = result.Errors.Any(error =>
            error.Code is "DuplicateEmail" or "DuplicateUserName");

        return new BadRequestObjectResult(new ValidationProblemDetails(errors)
        {
            Status = isDuplicate
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest,
            Title = isDuplicate ? "Account already exists" : "Account validation failed"
        })
        {
            StatusCode = isDuplicate
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest
        };
    }

    private sealed record AssignmentScopeResult(
        Guid? TenantId,
        Guid? MerchantId,
        BadRequestObjectResult? Error)
    {
        public static AssignmentScopeResult Global { get; } = new(null, null, null);

        public static AssignmentScopeResult ForTenant(Guid tenantId, Guid? merchantId)
        {
            return new AssignmentScopeResult(tenantId, merchantId, null);
        }

        public static AssignmentScopeResult Invalid(string detail)
        {
            return new AssignmentScopeResult(
                null,
                null,
                new BadRequestObjectResult(new ProblemDetails
                {
                    Title = "Invalid role assignment",
                    Detail = detail,
                    Status = StatusCodes.Status400BadRequest
                }));
        }
    }
}
