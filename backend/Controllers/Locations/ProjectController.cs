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
    private readonly IAuditService _auditService;

    public ProjectController(IProjectLocationService projectLocationService, IStockService stockService, IAuditService auditService)
    {
        _projectLocationService = projectLocationService;
        _stockService = stockService;
        _auditService = auditService;
    }
    [HttpPost]
    public async Task<ActionResult<ProjectLocationSummaryResponse>> CreateProject([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _projectLocationService.CreateProjectAsync(request.Name, request.Code, GetCurrentUserId(), cancellationToken);
            // Optionally log audit here if desired
            return Created($"/api/projects/{result.Id}", result);
        }
        catch (InvalidOperationException ex) when (ex.Message == "duplicate_project_code")
        {
            return BadRequest(new ErrorResponse { Code = "duplicate_project_code", Message = "A project with this code already exists." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse { Code = "invalid_project_data", Message = ex.Message });
        }
    }

    [HttpPut("{locationId:guid}/deactivate")]
    public async Task<ActionResult<ProjectLocationSummaryResponse>> DeactivateProject(Guid locationId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _projectLocationService.DeactivateProjectAsync(locationId, GetCurrentUserId(), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse { Code = "deactivate_project_failed", Message = ex.Message });
        }
    }

    [HttpPut("{locationId:guid}/activate")]
    public async Task<ActionResult<ProjectLocationSummaryResponse>> ActivateProject(Guid locationId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _projectLocationService.ActivateProjectAsync(locationId, GetCurrentUserId(), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse { Code = "activate_project_failed", Message = ex.Message });
        }
    }

    [HttpDelete("{locationId:guid}")]
    public async Task<ActionResult> DeleteProject(Guid locationId, CancellationToken cancellationToken)
    {
        try
        {
            // Get old values for audit
            var location = await _projectLocationService.GetProjectLocationAsync(locationId, requireActive: false, cancellationToken);
            await _projectLocationService.DeleteProjectAsync(locationId, cancellationToken);
            await _auditService.LogAsync(GetCurrentUserId(), "DELETE_PROJECT", "Project", locationId, oldValues: new { location.Name, location.Code }, newValues: null, cancellationToken: cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            var code = ex.Message == "project_has_stock" ? "project_has_stock" : "delete_project_failed";
            return BadRequest(new ErrorResponse { Code = code, Message = ex.Message });
        }
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