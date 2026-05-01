using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Locations;

public sealed record GetLocationsQuery(PagedQuery PagedQuery, Guid? ShelfId, Guid? AreaId, bool? IsActive) : IRequest<PaginatedResponse<LocationResponse>>;

public sealed class GetLocationsQueryHandler : IRequestHandler<GetLocationsQuery, PaginatedResponse<LocationResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetLocationsQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedResponse<LocationResponse>> Handle(GetLocationsQuery query, CancellationToken cancellationToken)
    {
        var page = query.PagedQuery.Page <= 0 ? 1 : query.PagedQuery.Page;
        var pageSize = query.PagedQuery.PageSize <= 0 ? 20 : Math.Min(query.PagedQuery.PageSize, 200);

        var locationsQuery = query.IsActive.HasValue
            ? _context.WarehouseLocations.IgnoreQueryFilters().Where(x => x.IsActive == query.IsActive.Value)
            : _context.WarehouseLocations.AsQueryable();

        if (query.ShelfId.HasValue)
        {
            locationsQuery = locationsQuery.Where(x => x.ShelfId == query.ShelfId.Value);
        }

        if (query.AreaId.HasValue)
        {
            locationsQuery = locationsQuery.Where(x => _context.WarehouseShelves
                .IgnoreQueryFilters()
                .Any(s => s.Id == x.ShelfId && s.AreaId == query.AreaId.Value));
        }

        locationsQuery = locationsQuery
            .AsNoTracking()
            .OrderBy(x => x.BinX)
            .ThenBy(x => x.BinY)
            .ThenBy(x => x.Name);

        var total = await locationsQuery.CountAsync(cancellationToken);
        var locations = await locationsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var shelfIds = locations.Select(x => x.ShelfId).Distinct().ToList();
        var areaByShelf = shelfIds.Count == 0
            ? new Dictionary<Guid, Guid>()
            : await _context.WarehouseShelves
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => shelfIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.AreaId, cancellationToken);

        var locationIds = locations.Select(x => x.Id).ToList();
        var stockByLocation = locationIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _context.StockLocations
                .AsNoTracking()
                .Where(x => locationIds.Contains(x.LocationId))
                .GroupBy(x => x.LocationId)
                .Select(g => new { LocationId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.LocationId, x => x.Quantity, cancellationToken);

        var mapped = _mapper.Map<List<LocationResponse>>(locations);
        foreach (var location in mapped)
        {
            location.AreaId = areaByShelf.GetValueOrDefault(location.ShelfId, Guid.Empty);
            location.CurrentStockQuantity = stockByLocation.GetValueOrDefault(location.Id, 0);
        }

        return new PaginatedResponse<LocationResponse>
        {
            Items = mapped,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }
}
