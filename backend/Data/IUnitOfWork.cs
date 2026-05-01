using Storage.Data.Repositories;

namespace Storage.Data;

public interface IUnitOfWork
{
    IRepository<WarehouseArea> WarehouseAreas { get; }
    IRepository<WarehouseShelf> WarehouseShelves { get; }
    IRepository<WarehouseLocation> WarehouseLocations { get; }
    IRepository<ComponentCategory> ComponentCategories { get; }
    IComponentRepository Components { get; }
    IRepository<ComponentType> ComponentTypes { get; }
    IRepository<Supplier> Suppliers { get; }
    IStockLocationRepository StockLocations { get; }
    IRepository<User> Users { get; }
    IAuditLogRepository AuditLogs { get; }
    IRepository<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
