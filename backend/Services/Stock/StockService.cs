using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Services.Stock;

public class StockService : IStockService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StockService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<StockLevelResponse> ReceiveStockAsync(Guid componentId, Guid locationId, int quantity, string? batchCode, DateTime? expiryDate, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        var component = await _context.Components.FirstOrDefaultAsync(x => x.Id == componentId, cancellationToken)
            ?? throw new KeyNotFoundException("Component was not found.");

        var location = await _context.WarehouseLocations.FirstOrDefaultAsync(x => x.Id == locationId, cancellationToken)
            ?? throw new KeyNotFoundException("Location was not found.");

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        var stock = await _context.StockLocations.FirstOrDefaultAsync(
            x => x.ComponentId == componentId && x.LocationId == locationId && x.BatchCode == batchCode,
            cancellationToken);

        if (stock is null)
        {
            stock = new StockLocation
            {
                Id = Guid.NewGuid(),
                ComponentId = componentId,
                LocationId = locationId,
                BinX = location.BinX,
                BinY = location.BinY,
                Quantity = 0,
                BatchCode = batchCode,
                ExpiryDate = expiryDate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                CreatedBy = GetActorId(),
                ModifiedBy = GetActorId()
            };

            await _context.StockLocations.AddAsync(stock, cancellationToken);
        }

        stock.Quantity += quantity;
        stock.LastUpdated = DateTime.UtcNow;
        stock.ModifiedAt = DateTime.UtcNow;
        stock.ModifiedBy = GetActorId();
        component.QuantityOnHand += quantity;
        component.LastReceivedDate = DateTime.UtcNow;
        component.ModifiedAt = DateTime.UtcNow;
        component.ModifiedBy = GetActorId();

        await WriteAuditLogAsync("STOCK_RECEIVE", "Component", component.Id, null, new { componentId, locationId, quantity, batchCode, expiryDate }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new StockLevelResponse
        {
            ComponentId = componentId,
            LocationId = locationId,
            Quantity = stock.Quantity,
            BatchCode = stock.BatchCode,
            ExpiryDate = stock.ExpiryDate
        };
    }

    public async Task<StockLevelResponse> GatherStockAsync(Guid componentId, Guid locationId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        var component = await _context.Components.FirstOrDefaultAsync(x => x.Id == componentId, cancellationToken)
            ?? throw new KeyNotFoundException("Component was not found.");

        var stock = await _context.StockLocations
            .OrderBy(x => x.ExpiryDate)
            .ThenBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.ComponentId == componentId && x.LocationId == locationId && x.Quantity > 0, cancellationToken)
            ?? throw new InvalidOperationException("No stock available at the selected location.");

        if (stock.Quantity < quantity)
        {
            throw new InvalidOperationException("Insufficient quantity at selected location.");
        }

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        stock.Quantity -= quantity;
        stock.LastUpdated = DateTime.UtcNow;
        stock.ModifiedAt = DateTime.UtcNow;
        stock.ModifiedBy = GetActorId();
        component.QuantityOnHand -= quantity;
        component.ModifiedAt = DateTime.UtcNow;
        component.ModifiedBy = GetActorId();

        await WriteAuditLogAsync("STOCK_GATHER", "Component", component.Id, null, new { componentId, locationId, quantity }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new StockLevelResponse
        {
            ComponentId = componentId,
            LocationId = locationId,
            Quantity = stock.Quantity,
            BatchCode = stock.BatchCode,
            ExpiryDate = stock.ExpiryDate
        };
    }

    public async Task TransferStockAsync(Guid componentId, Guid fromLocationId, Guid toLocationId, int quantity, CancellationToken cancellationToken = default)
    {
        if (fromLocationId == toLocationId)
        {
            throw new InvalidOperationException("Source and target locations must be different.");
        }

        var gathered = await GatherStockAsync(componentId, fromLocationId, quantity, cancellationToken);
        await ReceiveStockAsync(componentId, toLocationId, quantity, gathered.BatchCode, gathered.ExpiryDate, cancellationToken);

        await WriteAuditLogAsync("STOCK_TRANSFER", "Component", componentId, null, new { componentId, fromLocationId, toLocationId, quantity }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockLevelResponse>> GetStockLevelsAsync(Guid componentId, CancellationToken cancellationToken = default)
    {
        return await _context.StockLocations
            .AsNoTracking()
            .Where(x => x.ComponentId == componentId)
            .OrderByDescending(x => x.Quantity)
            .Select(x => new StockLevelResponse
            {
                ComponentId = x.ComponentId,
                LocationId = x.LocationId,
                Quantity = x.Quantity,
                BatchCode = x.BatchCode,
                ExpiryDate = x.ExpiryDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LocationInventoryItemResponse>> GetLocationInventoryAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        return await _context.StockLocations
            .AsNoTracking()
            .Where(x => x.LocationId == locationId && x.Quantity > 0)
            .Join(
                _context.Components.AsNoTracking(),
                stock => stock.ComponentId,
                component => component.Id,
                (stock, component) => new LocationInventoryItemResponse
                {
                    ComponentId = stock.ComponentId,
                    PartNumber = component.PartNumber,
                    Quantity = stock.Quantity,
                    BatchCode = stock.BatchCode,
                    ExpiryDate = stock.ExpiryDate
                })
            .OrderBy(x => x.PartNumber)
            .ToListAsync(cancellationToken);
    }

    private Guid GetActorId()
    {
        var rawUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(rawUserId, out var parsed) ? parsed : Guid.Empty;
    }

    private async Task WriteAuditLogAsync(string action, string entityType, Guid entityId, object? oldValues, object? newValues, CancellationToken cancellationToken)
    {
        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = GetActorId(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues is null ? null : System.Text.Json.JsonSerializer.Serialize(oldValues),
            NewValues = newValues is null ? null : System.Text.Json.JsonSerializer.Serialize(newValues),
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
            Timestamp = DateTime.UtcNow
        }, cancellationToken);
    }
}