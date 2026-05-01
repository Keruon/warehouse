using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storage.Helpers.DTOs;
using Storage.Services.Projects;
using Storage.Services.Stock;

namespace Storage.Controllers.Locations;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectController : ControllerBase
{
    private readonly IProjectLocationService _projectLocationService;
    private readonly IStockService _stockService;

    public ProjectController(IProjectLocationService projectLocationService, IStockService stockService)
    {
        _projectLocationService = projectLocationService;
        _stockService = stockService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectLocationSummaryResponse>>> GetProjects(CancellationToken cancellationToken)
    {
        var result = await _projectLocationService.ListProjectsAsync(GetCurrentUserIdOrNull(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("active")]
    public async Task<ActionResult<ActiveProjectResponse>> GetActiveProject(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return Ok(new ActiveProjectResponse
        {
            ActiveProject = await _projectLocationService.GetActiveProjectAsync(userId, cancellationToken)
        });
    }

    [HttpPut("active/{locationId:guid}")]
    public async Task<ActionResult<ProjectLocationSummaryResponse>> SetActiveProject(Guid locationId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _projectLocationService.SetActiveProjectAsync(GetCurrentUserId(), locationId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            return BadRequest(new ErrorResponse { Code = "active_project_failed", Message = ex.Message });
        }
    }

    [HttpDelete("active")]
    public async Task<ActionResult> ClearActiveProject(CancellationToken cancellationToken)
    {
        await _projectLocationService.ClearActiveProjectAsync(GetCurrentUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{locationId:guid}/close")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CloseProjectResponse>> CloseProject(Guid locationId, [FromBody] CloseProjectRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stockService.CloseProjectAsync(locationId, request.Confirm, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            return BadRequest(new ErrorResponse { Code = "close_project_failed", Message = ex.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out var userId))
        {
            throw new InvalidOperationException("Current user could not be resolved.");
        }

        return userId;
    }

    private Guid? GetCurrentUserIdOrNull()
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(rawUserId, out var userId) ? userId : null;
    }
}