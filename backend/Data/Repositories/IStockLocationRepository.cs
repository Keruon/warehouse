namespace Storage.Data.Repositories;

public sealed record StockSummaryItem(Guid ComponentId, string PartNumber, int Quantity);

public interface IStockLocationRepository : IRepository<StockLocation>
{
    Task<IReadOnlyList<StockLocation>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockLocation>> GetByComponentAsync(Guid componentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockSummaryItem>> GetStockSummaryAsync(CancellationToken cancellationToken = default);
}
