using Storage.Helpers.DTOs;

namespace Storage.Services.Stock;

public interface IStockService
{
    Task<StockLevelResponse> ReceiveStockAsync(Guid componentId, Guid locationId, int quantity, string? batchCode, DateTime? expiryDate, CancellationToken cancellationToken = default);
    Task<StockLevelResponse> GatherStockAsync(Guid componentId, Guid locationId, int quantity, CancellationToken cancellationToken = default);
    Task TransferStockAsync(Guid componentId, Guid fromLocationId, Guid toLocationId, int quantity, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockLevelResponse>> GetStockLevelsAsync(Guid componentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocationInventoryItemResponse>> GetLocationInventoryAsync(Guid locationId, CancellationToken cancellationToken = default);
}