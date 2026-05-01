using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Services.Search;

public class SearchService : ISearchService
{
    private readonly ApplicationDbContext _context;

    public SearchService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<ComponentResponse>> SearchComponentsAsync(string? query, ComponentSearchRequest filters, CancellationToken cancellationToken = default)
    {
        var page = filters.Page <= 0 ? 1 : filters.Page;
        var pageSize = filters.PageSize <= 0 ? 20 : Math.Min(filters.PageSize, 200);

        var componentsQuery = _context.Components.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            componentsQuery = componentsQuery.Where(x =>
                EF.Functions.ILike(x.PartNumber, $"%{query}%")
                || (x.ComponentTypeName != null && EF.Functions.ILike(x.ComponentTypeName, $"%{query}%"))
                || (x.SupplierName != null && EF.Functions.ILike(x.SupplierName, $"%{query}%"))
                || (x.BatchCode != null && EF.Functions.ILike(x.BatchCode, $"%{query}%")));
        }

        if (filters.TypeId.HasValue)
        {
            componentsQuery = componentsQuery.Where(x => x.ComponentTypeId == filters.TypeId.Value);
        }

        if (filters.SupplierId.HasValue)
        {
            componentsQuery = componentsQuery.Where(x => x.SupplierId == filters.SupplierId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.PartNumber))
        {
            componentsQuery = componentsQuery.Where(x => EF.Functions.ILike(x.PartNumber, $"%{filters.PartNumber}%"));
        }

        if (!string.IsNullOrWhiteSpace(filters.Manufacturer))
        {
            componentsQuery = componentsQuery.Where(x => x.SupplierName != null && EF.Functions.ILike(x.SupplierName, $"%{filters.Manufacturer}%"));
        }

        if (filters.CategoryId.HasValue)
        {
            var typeIds = await _context.ComponentTypes.AsNoTracking()
                .Where(x => x.CategoryId == filters.CategoryId.Value)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            componentsQuery = componentsQuery.Where(x => typeIds.Contains(x.ComponentTypeId));
        }

        componentsQuery = componentsQuery.OrderBy(x => x.PartNumber);

        var total = await componentsQuery.CountAsync(cancellationToken);
        var components = await componentsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var typeIdsForRows = components.Select(x => x.ComponentTypeId).Distinct().ToList();
        var typesById = typeIdsForRows.Count == 0
            ? new Dictionary<Guid, ComponentType>()
            : await _context.ComponentTypes.AsNoTracking()
                .Where(x => typeIdsForRows.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

        var responseItems = components.Select(component =>
        {
            var type = typesById.GetValueOrDefault(component.ComponentTypeId);
            return new ComponentResponse
            {
                Id = component.Id,
                ComponentTypeId = component.ComponentTypeId,
                ComponentTypeName = type?.Name ?? component.ComponentTypeName,
                CategoryId = type?.CategoryId ?? Guid.Empty,
                PartNumber = component.PartNumber,
                BatchCode = component.BatchCode,
                QuantityOnHand = component.QuantityOnHand,
                QuantityReserved = component.QuantityReserved,
                QuantityCommitted = component.QuantityCommitted,
                SupplierId = component.SupplierId,
                SupplierCode = component.SupplierCode,
                SupplierName = component.SupplierName,
                UnitCost = component.UnitCost,
                IsActive = component.IsActive
            };
        }).ToList();

        return new PaginatedResponse<ComponentResponse>
        {
            Items = responseItems,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<IReadOnlyList<LocationResponse>> SearchLocationsAsync(string? query, LocationSearchRequest filters, CancellationToken cancellationToken = default)
    {
        var locationQuery = _context.WarehouseLocations.AsNoTracking().AsQueryable();

        if (filters.ShelfId.HasValue)
        {
            locationQuery = locationQuery.Where(x => x.ShelfId == filters.ShelfId.Value);
        }

        if (filters.AreaId.HasValue)
        {
            locationQuery = locationQuery.Where(x => _context.WarehouseShelves
                .IgnoreQueryFilters()
                .Any(s => s.Id == x.ShelfId && s.AreaId == filters.AreaId.Value));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            locationQuery = locationQuery.Where(x =>
                EF.Functions.ILike(x.Name, $"%{query}%")
                || EF.Functions.ILike(x.Code, $"%{query}%")
                || (x.Description != null && EF.Functions.ILike(x.Description, $"%{query}%")));
        }

        var locations = await locationQuery
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (locations.Count == 0)
        {
            return [];
        }

        var shelfIds = locations.Select(x => x.ShelfId).Distinct().ToList();
        var areaByShelf = await _context.WarehouseShelves
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => shelfIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.AreaId, cancellationToken);

        var locationIds = locations.Select(x => x.Id).ToList();
        var stockByLocation = await _context.StockLocations
            .AsNoTracking()
            .Where(x => locationIds.Contains(x.LocationId))
            .GroupBy(x => x.LocationId)
            .Select(g => new { LocationId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.LocationId, x => x.Quantity, cancellationToken);

        var mapped = locations.Select(location => new LocationResponse
        {
            Id = location.Id,
            ShelfId = location.ShelfId,
            AreaId = areaByShelf.GetValueOrDefault(location.ShelfId, Guid.Empty),
            Name = location.Name,
            Code = location.Code,
            Description = location.Description,
            BinX = location.BinX,
            BinY = location.BinY,
            Depth = location.Depth,
            Width = location.Width,
            Height = location.Height,
            Volume = location.Volume,
            IsReserved = location.IsReserved,
            IsActive = location.IsActive,
            CurrentStockQuantity = stockByLocation.GetValueOrDefault(location.Id, 0)
        }).ToList();

        if (filters.HasStock.HasValue)
        {
            mapped = filters.HasStock.Value
                ? mapped.Where(x => x.CurrentStockQuantity > 0).ToList()
                : mapped.Where(x => x.CurrentStockQuantity == 0).ToList();
        }

        return mapped;
    }
}
