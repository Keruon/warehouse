using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Helpers.DTOs;
using Storage.Services.Auth;

namespace Storage.Controllers.Users;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;

    public AuthController(IAuthService authService, ApplicationDbContext context)
    {
        _authService = authService;
        _context = context;
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
    public async Task<ActionResult<object>> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = User.FindFirstValue(ClaimTypes.Name);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);
        ProjectLocationSummaryResponse? activeProject = null;

        if (Guid.TryParse(userId, out var parsedUserId))
        {
            activeProject = await _context.Users
                .AsNoTracking()
                .Where(x => x.Id == parsedUserId && x.ActiveProjectLocationId != null)
                .Join(
                    _context.WarehouseLocations.AsNoTracking(),
                    user => user.ActiveProjectLocationId,
                    location => (Guid?)location.Id,
                    (user, location) => new ProjectLocationSummaryResponse
                    {
                        Id = location.Id,
                        ShelfId = location.ShelfId,
                        AreaId = _context.WarehouseShelves.Where(shelf => shelf.Id == location.ShelfId).Select(shelf => shelf.AreaId).FirstOrDefault(),
                        Name = location.Name,
                        Code = location.Code,
                        IsActive = location.IsActive,
                        IsCurrentActiveProject = true
                    })
                .FirstOrDefaultAsync(cancellationToken);
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                userId,
                username,
                email,
                role,
                activeProject
            }
        });
    }
}
