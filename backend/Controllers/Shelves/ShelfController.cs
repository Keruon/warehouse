using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Shelves;

[ApiController]
[Route("api/shelves")]
[Authorize]
public class ShelfController : ControllerBase
{
    private readonly IMediator _mediator;

    public ShelfController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ShelfResponse>>> GetShelves(
        [FromQuery] PagedQuery pagedQuery,
        [FromQuery] Guid? areaId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetShelvesQuery(pagedQuery, areaId, isActive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShelfDetailsResponse>> GetShelfById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetShelfByIdQuery(id), cancellationToken);
        return result is null
            ? NotFound(new ErrorResponse { Code = "shelf_not_found", Message = "Shelf was not found." })
            : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ShelfResponse>> CreateShelf([FromBody] CreateShelfRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new CreateShelfCommand(request), cancellationToken);
            return CreatedAtAction(nameof(GetShelfById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Code = "area_not_found", Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse { Code = "duplicate_shelf", Message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ShelfResponse>> UpdateShelf(Guid id, [FromBody] UpdateShelfRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new UpdateShelfCommand(id, request), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            var code = ex.Message.Contains("Area", StringComparison.OrdinalIgnoreCase) ? "area_not_found" : "shelf_not_found";
            return NotFound(new ErrorResponse { Code = code, Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse { Code = "duplicate_shelf", Message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteShelf(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteShelfCommand(id), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Code = "shelf_not_found", Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse { Code = "shelf_has_locations", Message = ex.Message });
        }
    }
}
