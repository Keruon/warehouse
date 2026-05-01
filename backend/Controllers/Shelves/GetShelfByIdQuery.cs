using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Shelves;

public sealed record GetShelfByIdQuery(Guid ShelfId) : IRequest<ShelfDetailsResponse?>;

public sealed class GetShelfByIdQueryHandler : IRequestHandler<GetShelfByIdQuery, ShelfDetailsResponse?>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetShelfByIdQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ShelfDetailsResponse?> Handle(GetShelfByIdQuery query, CancellationToken cancellationToken)
    {
        var shelf = await _context.WarehouseShelves
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.ShelfId, cancellationToken);

        if (shelf is null)
        {
            return null;
        }

        var locations = await _context.WarehouseLocations
            .AsNoTracking()
            .Where(x => x.ShelfId == shelf.Id)
            .OrderBy(x => x.BinX)
            .ThenBy(x => x.BinY)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var locationIds = locations.Select(x => x.Id).ToList();
        var stockSummary = locationIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _context.StockLocations
                .AsNoTracking()
                .Where(x => locationIds.Contains(x.LocationId))
                .GroupBy(x => x.LocationId)
                .Select(g => new { LocationId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.LocationId, x => x.Quantity, cancellationToken);

        var mappedLocations = _mapper.Map<List<LocationResponse>>(locations);
        foreach (var location in mappedLocations)
        {
            location.AreaId = shelf.AreaId;
            location.CurrentStockQuantity = stockSummary.GetValueOrDefault(location.Id, 0);
        }

        var response = _mapper.Map<ShelfDetailsResponse>(shelf);
        response.LocationCount = mappedLocations.Count;
        response.Locations = mappedLocations;

        return response;
    }
}
