using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Areas;

public sealed record GetAreasQuery(PagedQuery PagedQuery, ZoneType? ZoneType, bool? IsActive) : IRequest<PaginatedResponse<AreaResponse>>;

public sealed class GetAreasQueryHandler : IRequestHandler<GetAreasQuery, PaginatedResponse<AreaResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAreasQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedResponse<AreaResponse>> Handle(GetAreasQuery query, CancellationToken cancellationToken)
    {
        var page = query.PagedQuery.Page <= 0 ? 1 : query.PagedQuery.Page;
        var pageSize = query.PagedQuery.PageSize <= 0 ? 20 : Math.Min(query.PagedQuery.PageSize, 200);

        var areasQuery = query.IsActive.HasValue
            ? _context.WarehouseAreas.IgnoreQueryFilters().Where(x => x.IsActive == query.IsActive.Value)
            : _context.WarehouseAreas.AsQueryable();

        if (query.ZoneType.HasValue)
        {
            areasQuery = areasQuery.Where(x => x.ZoneType == query.ZoneType.Value);
        }

        areasQuery = areasQuery
            .AsNoTracking()
            .OrderBy(x => x.FloorLevel)
            .ThenBy(x => x.Name);

        var total = await areasQuery.CountAsync(cancellationToken);
        var areas = await areasQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var areaIds = areas.Select(x => x.Id).ToList();
        var shelfCounts = await _context.WarehouseShelves
            .AsNoTracking()
            .Where(x => areaIds.Contains(x.AreaId))
            .GroupBy(x => x.AreaId)
            .Select(g => new { AreaId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AreaId, x => x.Count, cancellationToken);

        var mapped = _mapper.Map<List<AreaResponse>>(areas);
        foreach (var area in mapped)
        {
            area.ShelfCount = shelfCounts.GetValueOrDefault(area.Id, 0);
        }

        return new PaginatedResponse<AreaResponse>
        {
            Items = mapped,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }
}
