using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Helpers.DTOs;
using Storage.Services.Projects;

namespace Storage.Services.Stock;

public class StockService : IStockService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IProjectLocationService _projectLocationService;

    public StockService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, IProjectLocationService projectLocationService)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _projectLocationService = projectLocationService;
    }

    public async Task<StockLevelResponse> ReceiveStockAsync(Guid componentId, Guid locationId, int quantity, string? batchCode, DateTime? expiryDate, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        var component = await _context.Components.FirstOrDefaultAsync(x => x.Id == componentId, cancellationToken)
            ?? throw new KeyNotFoundException("Component was not found.");

        var location = await GetLocationAsync(locationId, cancellationToken);

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        var stock = await GetOrCreateStockLineAsync(componentId, location, batchCode, expiryDate, cancellationToken);

        stock.Quantity += quantity;
        TouchStockLine(stock);
        component.QuantityOnHand += quantity;
        component.LastReceivedDate = DateTime.UtcNow;
        component.ModifiedAt = DateTime.UtcNow;
        component.ModifiedBy = GetActorId();

        await WriteAuditLogAsync("STOCK_RECEIVE", "Component", component.Id, null, new { componentId, locationId, quantity, batchCode, expiryDate }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return await BuildStockLevelResponseAsync(stock, location, cancellationToken);
    }

    public async Task<StockLevelResponse> GatherStockAsync(Guid componentId, Guid locationId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        var component = await _context.Components.FirstOrDefaultAsync(x => x.Id == componentId, cancellationToken)
            ?? throw new KeyNotFoundException("Component was not found.");

        var sourceLocation = await GetLocationAsync(locationId, cancellationToken);
        if (sourceLocation.LocationKind == LocationKind.Project)
        {
            throw new InvalidOperationException("Project locations cannot be used as gather sources.");
        }

        var stock = await GetSourceStockLineAsync(componentId, locationId, cancellationToken);

        var activeProjectLocationId = await GetActiveProjectLocationIdAsync(cancellationToken);
        if (activeProjectLocationId.HasValue)
        {
            var targetLocation = await _projectLocationService.GetProjectLocationAsync(activeProjectLocationId.Value, requireActive: true, cancellationToken);

            await using var transferTx = await _context.Database.BeginTransactionAsync(cancellationToken);
            var targetStock = await MoveStockLineAsync(
                stock,
                targetLocation,
                quantity,
                "STOCK_GATHER_TO_PROJECT",
                new
                {
                    componentId,
                    fromLocationId = locationId,
                    toLocationId = targetLocation.Id,
                    quantity,
                    stockLocationId = stock.Id
                },
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await transferTx.CommitAsync(cancellationToken);
            return await BuildStockLevelResponseAsync(targetStock, targetLocation, cancellationToken);
        }

        if (stock.Quantity < quantity)
        {
            throw new InvalidOperationException("Insufficient quantity at selected location.");
        }

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        stock.Quantity -= quantity;
    TouchStockLine(stock);
        component.QuantityOnHand -= quantity;
        component.ModifiedAt = DateTime.UtcNow;
        component.ModifiedBy = GetActorId();

        await WriteAuditLogAsync("STOCK_GATHER", "Component", component.Id, null, new { componentId, locationId, quantity }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return await BuildStockLevelResponseAsync(stock, sourceLocation, cancellationToken);
    }

    public async Task TransferStockAsync(Guid componentId, Guid fromLocationId, Guid toLocationId, int quantity, CancellationToken cancellationToken = default)
    {
        if (fromLocationId == toLocationId)
        {
            throw new InvalidOperationException("Source and target locations must be different.");
        }

        var fromStock = await GetSourceStockLineAsync(componentId, fromLocationId, cancellationToken);
        var targetLocation = await GetLocationAsync(toLocationId, cancellationToken);

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        await MoveStockLineAsync(
            fromStock,
            targetLocation,
            quantity,
            "STOCK_TRANSFER",
            new { componentId, fromLocationId, toLocationId, quantity, stockLocationId = fromStock.Id },
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task<StockLevelResponse> ReturnProjectStockAsync(Guid stockLocationId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        var sourceStock = await GetStockLineByIdAsync(stockLocationId, cancellationToken);
        var sourceLocation = await GetLocationAsync(sourceStock.LocationId, cancellationToken);
        if (sourceLocation.LocationKind != LocationKind.Project)
        {
            throw new InvalidOperationException("Only project stock lines can be returned using this workflow.");
        }

        var targetLocation = await ResolveWarehouseReturnLocationAsync(sourceStock, cancellationToken);

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        var targetStock = await MoveStockLineAsync(
            sourceStock,
            targetLocation,
            quantity,
            "STOCK_RETURN_FROM_PROJECT",
            new
            {
                componentId = sourceStock.ComponentId,
                fromLocationId = sourceLocation.Id,
                toLocationId = targetLocation.Id,
                quantity,
                stockLocationId = sourceStock.Id
            },
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return await BuildStockLevelResponseAsync(targetStock, targetLocation, cancellationToken);
    }

    public async Task<CloseProjectResponse> CloseProjectAsync(Guid projectLocationId, bool confirm, CancellationToken cancellationToken = default)
    {
        if (!confirm)
        {
            throw new InvalidOperationException("Project close requires explicit confirmation.");
        }

        var projectLocation = await _projectLocationService.GetProjectLocationAsync(projectLocationId, requireActive: true, cancellationToken);
        var projectStockLines = await _context.StockLocations
            .Where(x => x.LocationId == projectLocationId && x.Quantity > 0)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var returnedLineCount = 0;
        var returnedQuantity = 0;

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        foreach (var stockLine in projectStockLines)
        {
            var lineQuantity = stockLine.Quantity;
            var targetLocation = await ResolveWarehouseReturnLocationAsync(stockLine, cancellationToken);
            await MoveStockLineAsync(
                stockLine,
                targetLocation,
                lineQuantity,
                "STOCK_RETURN_FROM_PROJECT",
                new
                {
                    componentId = stockLine.ComponentId,
                    fromLocationId = projectLocationId,
                    toLocationId = targetLocation.Id,
                    quantity = lineQuantity,
                    stockLocationId = stockLine.Id,
                    projectClose = true
                },
                cancellationToken);

            returnedLineCount += 1;
            returnedQuantity += lineQuantity;
        }

        projectLocation.IsActive = false;
        projectLocation.ModifiedAt = DateTime.UtcNow;
        projectLocation.ModifiedBy = GetActorId();

        var impactedUsers = await _context.Users.Where(x => x.ActiveProjectLocationId == projectLocationId).ToListAsync(cancellationToken);
        foreach (var user in impactedUsers)
        {
            user.ActiveProjectLocationId = null;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = GetActorId();
        }

        await WriteAuditLogAsync("PROJECT_CLOSE", "WarehouseLocation", projectLocationId, null, new
        {
            projectLocationId,
            projectName = projectLocation.Name,
            returnedLineCount,
            returnedQuantity
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new CloseProjectResponse
        {
            ProjectLocationId = projectLocationId,
            ProjectName = projectLocation.Name,
            ReturnedLineCount = returnedLineCount,
            ReturnedQuantity = returnedQuantity,
            Closed = true
        };
    }

    public async Task<IReadOnlyList<StockLevelResponse>> GetStockLevelsAsync(Guid componentId, CancellationToken cancellationToken = default)
    {
        return await _context.StockLocations
            .AsNoTracking()
            .Where(x => x.ComponentId == componentId)
            .OrderByDescending(x => x.Quantity)
            .Join(
                _context.WarehouseLocations.AsNoTracking(),
                stock => stock.LocationId,
                loc => loc.Id,
                (stock, loc) => new StockLevelResponse
                {
                    StockLocationId = stock.Id,
                    ComponentId = stock.ComponentId,
                    LocationId = stock.LocationId,
                    LocationName = loc.Name,
                    LocationCode = loc.Code,
                    LocationKind = loc.LocationKind,
                    Quantity = stock.Quantity,
                    BatchCode = stock.BatchCode,
                    ExpiryDate = stock.ExpiryDate
                })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LocationInventoryItemResponse>> GetLocationInventoryAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        var items = await _context.StockLocations
            .AsNoTracking()
            .Where(x => x.LocationId == locationId && x.Quantity > 0)
            .Join(
                _context.WarehouseLocations.AsNoTracking(),
                stock => stock.LocationId,
                location => location.Id,
                (stock, location) => new { stock, location })
            .Join(
                _context.Components.AsNoTracking(),
                joined => joined.stock.ComponentId,
                component => component.Id,
                (joined, component) => new LocationInventoryItemResponse
                {
                    StockLocationId = joined.stock.Id,
                    LocationId = joined.stock.LocationId,
                    LocationName = joined.location.Name,
                    LocationCode = joined.location.Code,
                    LocationKind = joined.location.LocationKind,
                    ComponentId = joined.stock.ComponentId,
                    PartNumber = component.PartNumber,
                    Quantity = joined.stock.Quantity,
                    BatchCode = joined.stock.BatchCode,
                    ExpiryDate = joined.stock.ExpiryDate
                })
            .OrderBy(x => x.PartNumber)
            .ToListAsync(cancellationToken);

        foreach (var item in items.Where(x => x.LocationKind == LocationKind.Project))
        {
            var sourceStock = await GetStockLineByIdAsync(item.StockLocationId, cancellationToken);
            var targetLocation = await ResolveWarehouseReturnLocationAsync(sourceStock, cancellationToken);
            item.SuggestedReturnLocationId = targetLocation.Id;
            item.SuggestedReturnLocationName = targetLocation.Name;
            item.SuggestedReturnLocationCode = targetLocation.Code;
        }

        return items;
    }

    private async Task<WarehouseLocation> GetLocationAsync(Guid locationId, CancellationToken cancellationToken)
    {
        return await _context.WarehouseLocations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == locationId, cancellationToken)
            ?? throw new KeyNotFoundException("Location was not found.");
    }

    private async Task<StockLocation> GetSourceStockLineAsync(Guid componentId, Guid locationId, CancellationToken cancellationToken)
    {
        return await _context.StockLocations
            .OrderBy(x => x.ExpiryDate)
            .ThenBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.ComponentId == componentId && x.LocationId == locationId && x.Quantity > 0, cancellationToken)
            ?? throw new InvalidOperationException("No stock available at the selected location.");
    }

    private async Task<StockLocation> GetStockLineByIdAsync(Guid stockLocationId, CancellationToken cancellationToken)
    {
        return await _context.StockLocations
            .FirstOrDefaultAsync(x => x.Id == stockLocationId && x.Quantity > 0, cancellationToken)
            ?? throw new KeyNotFoundException("Stock line was not found.");
    }

    private async Task<StockLocation> GetOrCreateStockLineAsync(Guid componentId, WarehouseLocation location, string? batchCode, DateTime? expiryDate, CancellationToken cancellationToken)
    {
        var stock = await _context.StockLocations.FirstOrDefaultAsync(
            x => x.ComponentId == componentId && x.LocationId == location.Id && x.BatchCode == batchCode,
            cancellationToken);

        if (stock is not null)
        {
            return stock;
        }

        stock = new StockLocation
        {
            Id = Guid.NewGuid(),
            ComponentId = componentId,
            LocationId = location.Id,
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
        return stock;
    }

    private async Task<StockLocation> MoveStockLineAsync(StockLocation sourceStock, WarehouseLocation targetLocation, int quantity, string action, object payload, CancellationToken cancellationToken)
    {
        if (sourceStock.Quantity < quantity)
        {
            throw new InvalidOperationException("Insufficient quantity at selected location.");
        }

        if (sourceStock.LocationId == targetLocation.Id)
        {
            throw new InvalidOperationException("Source and target locations must be different.");
        }

        var targetStock = await GetOrCreateStockLineAsync(sourceStock.ComponentId, targetLocation, sourceStock.BatchCode, sourceStock.ExpiryDate, cancellationToken);

        sourceStock.Quantity -= quantity;
        TouchStockLine(sourceStock);

        targetStock.Quantity += quantity;
        TouchStockLine(targetStock);

        await WriteAuditLogAsync(action, "Component", sourceStock.ComponentId, null, payload, cancellationToken);
        return targetStock;
    }

    private async Task<Guid?> GetActiveProjectLocationIdAsync(CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        if (actorId == Guid.Empty)
        {
            return null;
        }

        return await _context.Users
            .AsNoTracking()
            .Where(x => x.Id == actorId)
            .Select(x => x.ActiveProjectLocationId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<WarehouseLocation> ResolveWarehouseReturnLocationAsync(StockLocation sourceStock, CancellationToken cancellationToken)
    {
        var matchingLocationId = await _context.StockLocations
            .AsNoTracking()
            .Where(x => x.ComponentId == sourceStock.ComponentId && x.BatchCode == sourceStock.BatchCode && x.LocationId != sourceStock.LocationId)
            .Join(
                _context.WarehouseLocations.AsNoTracking().Where(x => x.LocationKind == LocationKind.Warehouse),
                stock => stock.LocationId,
                location => location.Id,
                (stock, location) => new { stock.LocationId, stock.ExpiryDate, location.BinX, location.BinY, location.Name })
            .Where(x => x.ExpiryDate == sourceStock.ExpiryDate)
            .OrderBy(x => x.BinX)
            .ThenBy(x => x.BinY)
            .ThenBy(x => x.Name)
            .Select(x => (Guid?)x.LocationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (matchingLocationId.HasValue)
        {
            return await GetLocationAsync(matchingLocationId.Value, cancellationToken);
        }

        return await _context.WarehouseLocations
            .AsNoTracking()
            .Where(x => x.LocationKind == LocationKind.Warehouse)
            .OrderBy(x => x.BinX)
            .ThenBy(x => x.BinY)
            .ThenBy(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No warehouse location is available for returned stock.");
    }

    private void TouchStockLine(StockLocation stock)
    {
        stock.LastUpdated = DateTime.UtcNow;
        stock.ModifiedAt = DateTime.UtcNow;
        stock.ModifiedBy = GetActorId();
    }

    private async Task<StockLevelResponse> BuildStockLevelResponseAsync(StockLocation stock, WarehouseLocation? knownLocation, CancellationToken cancellationToken)
    {
        var location = knownLocation ?? await GetLocationAsync(stock.LocationId, cancellationToken);
        return new StockLevelResponse
        {
            StockLocationId = stock.Id,
            ComponentId = stock.ComponentId,
            LocationId = stock.LocationId,
            LocationName = location.Name,
            LocationCode = location.Code,
            LocationKind = location.LocationKind,
            Quantity = stock.Quantity,
            BatchCode = stock.BatchCode,
            ExpiryDate = stock.ExpiryDate
        };
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