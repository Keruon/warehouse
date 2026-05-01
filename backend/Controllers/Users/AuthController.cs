using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storage.Helpers.DTOs;
using Storage.Services.Auth;

namespace Storage.Controllers.Users;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<object>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        if (result is null)
        {
            return Unauthorized(new ErrorResponse
            {
                Code = "invalid_credentials",
                Message = "Invalid username/email or password."
            });
        }

        return Ok(new { success = true, data = result });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<object>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request, cancellationToken);
        if (result is null)
        {
            return Unauthorized(new ErrorResponse
            {
                Code = "invalid_refresh_token",
                Message = "Refresh token is invalid, revoked, expired, or fingerprint does not match."
            });
        }

        return Ok(new { success = true, data = result });
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<ActionResult<object>> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var revoked = await _authService.RevokeTokenAsync(request.RefreshToken, cancellationToken);
        if (!revoked)
        {
            return NotFound(new ErrorResponse
            {
                Code = "refresh_token_not_found",
                Message = "Refresh token was not found or already revoked."
            });
        }

        return Ok(new { success = true, message = "Logged out successfully." });
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<object> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = User.FindFirstValue(ClaimTypes.Name);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new
        {
            success = true,
            data = new
            {
                userId,
                username,
                email,
                role
            }
        });
    }
}
