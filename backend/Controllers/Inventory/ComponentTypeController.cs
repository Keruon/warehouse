using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Inventory;

[ApiController]
[Route("api/component-types")]
[Authorize]
public class ComponentTypeController : ControllerBase
{
    private readonly IMediator _mediator;

    public ComponentTypeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ComponentTypeResponse>>> GetTypes(
        [FromQuery] PagedQuery pagedQuery,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? partNumber,
        [FromQuery] string? manufacturer,
        [FromQuery] string? stockSystemCode,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetComponentTypesQuery(pagedQuery, categoryId, partNumber, manufacturer, stockSystemCode, isActive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ComponentTypeResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetComponentTypeByIdQuery(id), cancellationToken);
        return result is null
            ? NotFound(new ErrorResponse { Code = "component_type_not_found", Message = "Component type was not found." })
            : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ComponentTypeResponse>> Create([FromBody] CreateComponentTypeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new CreateComponentTypeCommand(request), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Code = "category_not_found", Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse { Code = "duplicate_component_type", Message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ComponentTypeResponse>> Update(Guid id, [FromBody] UpdateComponentTypeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new UpdateComponentTypeCommand(id, request), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Code = "component_type_not_found", Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse { Code = "duplicate_component_type", Message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteComponentTypeCommand(id), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Code = "component_type_not_found", Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse { Code = "component_type_delete_blocked", Message = ex.Message });
        }
    }
}
