using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storage.Helpers.DTOs;
using Storage.Services.Stock;

namespace Storage.Controllers.Items;

[ApiController]
[Route("api/stock")]
[Authorize]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;

    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }

    [HttpPost("receive")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StockLevelResponse>> Receive([FromBody] ReceiveStockRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stockService.ReceiveStockAsync(request.ComponentId, request.LocationId, request.Quantity, request.BatchCode, request.ExpiryDate, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            return BadRequest(new ErrorResponse { Code = "stock_receive_failed", Message = ex.Message });
        }
    }

    [HttpPost("gather")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StockLevelResponse>> Gather([FromBody] GatherStockRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stockService.GatherStockAsync(request.ComponentId, request.LocationId, request.Quantity, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            return BadRequest(new ErrorResponse { Code = "stock_gather_failed", Message = ex.Message });
        }
    }

    [HttpPost("transfer")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Transfer([FromBody] TransferStockRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _stockService.TransferStockAsync(request.ComponentId, request.FromLocationId, request.ToLocationId, request.Quantity, cancellationToken);
            return Ok(new { success = true });
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            return BadRequest(new ErrorResponse { Code = "stock_transfer_failed", Message = ex.Message });
        }
    }

    [HttpPost("project-return")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StockLevelResponse>> ReturnProjectStock([FromBody] ReturnProjectStockRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stockService.ReturnProjectStockAsync(request.StockLocationId, request.Quantity, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            return BadRequest(new ErrorResponse { Code = "project_return_failed", Message = ex.Message });
        }
    }

    [HttpPost("bulk-transfer")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> BulkTransfer([FromBody] BulkTransferRequest request, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var item in request.Items)
            {
                await _stockService.TransferStockAsync(item.ComponentId, request.FromLocationId, request.ToLocationId, item.Quantity, cancellationToken);
            }

            return Ok(new { success = true, transferredItems = request.Items.Count });
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            return BadRequest(new ErrorResponse { Code = "bulk_transfer_failed", Message = ex.Message });
        }
    }

    [HttpGet("component/{id:guid}")]
    public async Task<ActionResult<IReadOnlyList<StockLevelResponse>>> GetByComponent(Guid id, CancellationToken cancellationToken)
    {
        var result = await _stockService.GetStockLevelsAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("location/{id:guid}")]
    public async Task<ActionResult<IReadOnlyList<LocationInventoryItemResponse>>> GetByLocation(Guid id, CancellationToken cancellationToken)
    {
        var result = await _stockService.GetLocationInventoryAsync(id, cancellationToken);
        return Ok(result);
    }
}
