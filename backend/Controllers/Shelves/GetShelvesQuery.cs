using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Shelves;

public sealed record GetShelvesQuery(PagedQuery PagedQuery, Guid? AreaId, bool? IsActive) : IRequest<PaginatedResponse<ShelfResponse>>;

public sealed class GetShelvesQueryHandler : IRequestHandler<GetShelvesQuery, PaginatedResponse<ShelfResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetShelvesQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedResponse<ShelfResponse>> Handle(GetShelvesQuery query, CancellationToken cancellationToken)
    {
        var page = query.PagedQuery.Page <= 0 ? 1 : query.PagedQuery.Page;
        var pageSize = query.PagedQuery.PageSize <= 0 ? 20 : Math.Min(query.PagedQuery.PageSize, 200);

        var shelvesQuery = query.IsActive.HasValue
            ? _context.WarehouseShelves.IgnoreQueryFilters().Where(x => x.IsActive == query.IsActive.Value)
            : _context.WarehouseShelves.AsQueryable();

        if (query.AreaId.HasValue)
        {
            shelvesQuery = shelvesQuery.Where(x => x.AreaId == query.AreaId.Value);
        }

        shelvesQuery = shelvesQuery
            .AsNoTracking()
            .OrderBy(x => x.Name);

        var total = await shelvesQuery.CountAsync(cancellationToken);
        var shelves = await shelvesQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var shelfIds = shelves.Select(x => x.Id).ToList();
        var locationCounts = shelfIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _context.WarehouseLocations
                .AsNoTracking()
                .Where(x => shelfIds.Contains(x.ShelfId))
                .GroupBy(x => x.ShelfId)
                .Select(g => new { ShelfId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ShelfId, x => x.Count, cancellationToken);

        var mapped = _mapper.Map<List<ShelfResponse>>(shelves);
        foreach (var shelf in mapped)
        {
            shelf.LocationCount = locationCounts.GetValueOrDefault(shelf.Id, 0);
        }

        return new PaginatedResponse<ShelfResponse>
        {
            Items = mapped,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }
}
