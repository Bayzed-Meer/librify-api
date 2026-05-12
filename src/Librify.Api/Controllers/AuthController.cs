using Librify.Application.Auth.Dtos;
using Librify.Application.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Librify.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[ProducesErrorResponseType(typeof(ProblemDetails))]
public class AuthController(IAuthService authService) : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";

    [HttpPost("register")]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        return Created(string.Empty, result);
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
        return Ok(new AuthResponse { AccessToken = result.AccessToken, ExpiresAt = result.AccessTokenExpiresAt });
    }

    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken = default)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        var result = await authService.RefreshTokenAsync(refreshToken, cancellationToken);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
        return Ok(new AuthResponse { AccessToken = result.AccessToken, ExpiresAt = result.AccessTokenExpiresAt });
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        await authService.LogoutAsync(refreshToken, cancellationToken);

        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = "/api/auth" });
        return NoContent();
    }

    private void SetRefreshTokenCookie(string token, DateTimeOffset expires)
    {
        Response.Cookies.Append(RefreshTokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = expires,
        });
    }
}
