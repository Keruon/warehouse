using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Locations;

[ApiController]
[Route("api/locations")]
[Authorize]
public class LocationController : ControllerBase
{
    private readonly IMediator _mediator;

    public LocationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<LocationResponse>>> GetLocations(
        [FromQuery] PagedQuery pagedQuery,
        [FromQuery] Guid? shelfId,
        [FromQuery] Guid? areaId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetLocationsQuery(pagedQuery, shelfId, areaId, isActive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LocationResponse>> GetLocationById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetLocationByIdQuery(id), cancellationToken);
        return result is null
            ? NotFound(new ErrorResponse { Code = "location_not_found", Message = "Location was not found." })
            : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LocationResponse>> CreateLocation([FromBody] CreateLocationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new CreateLocationCommand(request), cancellationToken);
            return CreatedAtAction(nameof(GetLocationById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Code = "shelf_not_found", Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse { Code = "duplicate_location", Message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LocationResponse>> UpdateLocation(Guid id, [FromBody] UpdateLocationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new UpdateLocationCommand(id, request), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            var code = ex.Message.Contains("Shelf", StringComparison.OrdinalIgnoreCase) ? "shelf_not_found" : "location_not_found";
            return NotFound(new ErrorResponse { Code = code, Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse { Code = "duplicate_location", Message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteLocation(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteLocationCommand(id), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Code = "location_not_found", Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse { Code = "location_has_stock", Message = ex.Message });
        }
    }
}
