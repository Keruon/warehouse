using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Areas;

public sealed record GetAreaByIdQuery(Guid AreaId) : IRequest<AreaDetailsResponse?>;

public sealed class GetAreaByIdQueryHandler : IRequestHandler<GetAreaByIdQuery, AreaDetailsResponse?>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAreaByIdQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<AreaDetailsResponse?> Handle(GetAreaByIdQuery query, CancellationToken cancellationToken)
    {
        var area = await _context.WarehouseAreas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.AreaId, cancellationToken);

        if (area is null)
        {
            return null;
        }

        var shelves = await _context.WarehouseShelves
            .AsNoTracking()
            .Where(x => x.AreaId == area.Id)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var locationCounts = await _context.WarehouseLocations
            .AsNoTracking()
            .Where(x => shelves.Select(s => s.Id).Contains(x.ShelfId))
            .GroupBy(x => x.ShelfId)
            .Select(g => new { ShelfId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ShelfId, x => x.Count, cancellationToken);

        var mappedShelves = _mapper.Map<List<ShelfResponse>>(shelves);
        foreach (var shelf in mappedShelves)
        {
            shelf.LocationCount = locationCounts.GetValueOrDefault(shelf.Id, 0);
        }

        var response = _mapper.Map<AreaDetailsResponse>(area);
        response.ShelfCount = mappedShelves.Count;
        response.Shelves = mappedShelves;

        return response;
    }
}
