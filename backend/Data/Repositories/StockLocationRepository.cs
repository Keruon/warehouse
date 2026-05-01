using Microsoft.EntityFrameworkCore;

namespace Storage.Data.Repositories;

public class StockLocationRepository : Repository<StockLocation>, IStockLocationRepository
{
    public StockLocationRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<StockLocation>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.LocationId == locationId)
            .OrderByDescending(x => x.LastUpdated)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockLocation>> GetByComponentAsync(Guid componentId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.ComponentId == componentId)
            .OrderByDescending(x => x.LastUpdated)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockSummaryItem>> GetStockSummaryAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Join(
                Context.Components.AsNoTracking(),
                stock => stock.ComponentId,
                component => component.Id,
                (stock, component) => new { stock.ComponentId, component.PartNumber, stock.Quantity })
            .GroupBy(x => new { x.ComponentId, x.PartNumber })
            .Select(group => new StockSummaryItem(group.Key.ComponentId, group.Key.PartNumber, group.Sum(x => x.Quantity)))
            .OrderBy(x => x.PartNumber)
            .ToListAsync(cancellationToken);
    }
}
