using Autonomy.Api.Authorization;
using Autonomy.Api.Contracts.Auth;
using Autonomy.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autonomy.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    private const string RefreshCookieName = "autonomy.refresh";

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AccessTokenResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (request.MerchantId.HasValue && !request.TenantId.HasValue)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid tenant context",
                Detail = "A MerchantId requires its parent TenantId (fleet ID)."
            });
        }

        var result = await authService.LoginAsync(request, cancellationToken);
        if (result.RequiresTenantSelection)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Tenant selection required",
                Detail = "Select a tenant associated with this account."
            });
        }

        if (result.Session is null)
        {
            return Unauthorized();
        }

        SetRefreshCookie(result.Session.RefreshTokenValue, result.Session.RefreshTokenExpiresAtUtc);
        return Ok(result.Session.AccessToken);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AccessTokenResponse>> Refresh(
        CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var rawRefreshToken)
            || string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return Unauthorized();
        }

        var session = await authService.RefreshAsync(rawRefreshToken, cancellationToken);
        if (session is null)
        {
            ClearRefreshCookie();
            return Unauthorized();
        }

        SetRefreshCookie(session.RefreshTokenValue, session.RefreshTokenExpiresAtUtc);
        return Ok(session.AccessToken);
    }

    [Authorize]
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Revoke(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId))
        {
            return Unauthorized();
        }

        if (Request.Cookies.TryGetValue(RefreshCookieName, out var rawRefreshToken)
            && !string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            await authService.RevokeAsync(userId, rawRefreshToken, cancellationToken);
        }

        ClearRefreshCookie();
        return NoContent();
    }

    private void SetRefreshCookie(string token, DateTime expiresAtUtc)
    {
        Response.Cookies.Append(
            RefreshCookieName,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Expires = new DateTimeOffset(DateTime.SpecifyKind(
                    expiresAtUtc,
                    DateTimeKind.Utc)),
                Path = "/api/v1/auth"
            });
    }

    private void ClearRefreshCookie()
    {
        Response.Cookies.Delete(
            RefreshCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/api/v1/auth"
            });
    }
}
