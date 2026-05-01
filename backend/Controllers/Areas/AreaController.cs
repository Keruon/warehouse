using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Areas;

[ApiController]
[Route("api/areas")]
[Authorize]
public class AreaController : ControllerBase
{
    private readonly IMediator _mediator;

    public AreaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AreaResponse>>> GetAreas(
        [FromQuery] PagedQuery pagedQuery,
        [FromQuery] ZoneType? zoneType,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAreasQuery(pagedQuery, zoneType, isActive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AreaDetailsResponse>> GetAreaById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAreaByIdQuery(id), cancellationToken);
        return result is null
            ? NotFound(new ErrorResponse { Code = "area_not_found", Message = "Area was not found." })
            : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AreaResponse>> CreateArea([FromBody] CreateAreaRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new CreateAreaCommand(request), cancellationToken);
            return CreatedAtAction(nameof(GetAreaById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse { Code = "duplicate_area", Message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AreaResponse>> UpdateArea(Guid id, [FromBody] UpdateAreaRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new UpdateAreaCommand(id, request), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Code = "area_not_found", Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse { Code = "duplicate_area", Message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteArea(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteAreaCommand(id), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Code = "area_not_found", Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse { Code = "area_has_shelves", Message = ex.Message });
        }
    }
}
