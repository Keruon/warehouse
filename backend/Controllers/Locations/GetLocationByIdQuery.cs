using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Locations;

public sealed record GetLocationByIdQuery(Guid LocationId) : IRequest<LocationResponse?>;

public sealed class GetLocationByIdQueryHandler : IRequestHandler<GetLocationByIdQuery, LocationResponse?>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetLocationByIdQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<LocationResponse?> Handle(GetLocationByIdQuery query, CancellationToken cancellationToken)
    {
        var location = await _context.WarehouseLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.LocationId, cancellationToken);

        if (location is null)
        {
            return null;
        }

        var shelf = await _context.WarehouseShelves
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == location.ShelfId, cancellationToken);

        var currentStock = await _context.StockLocations
            .AsNoTracking()
            .Where(x => x.LocationId == location.Id)
            .SumAsync(x => x.Quantity, cancellationToken);

        var response = _mapper.Map<LocationResponse>(location);
        response.AreaId = shelf?.AreaId ?? Guid.Empty;
        response.CurrentStockQuantity = currentStock;
        return response;
    }
}
