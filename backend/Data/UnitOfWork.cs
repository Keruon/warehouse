using Storage.Data.Repositories;

namespace Storage.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    private IRepository<WarehouseArea>? _warehouseAreas;
    private IRepository<WarehouseShelf>? _warehouseShelves;
    private IRepository<WarehouseLocation>? _warehouseLocations;
    private IRepository<ComponentCategory>? _componentCategories;
    private IComponentRepository? _components;
    private IRepository<ComponentType>? _componentTypes;
    private IRepository<Supplier>? _suppliers;
    private IStockLocationRepository? _stockLocations;
    private IRepository<User>? _users;
    private IAuditLogRepository? _auditLogs;
    private IRepository<RefreshToken>? _refreshTokens;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IRepository<WarehouseArea> WarehouseAreas => _warehouseAreas ??= new Repository<WarehouseArea>(_context);
    public IRepository<WarehouseShelf> WarehouseShelves => _warehouseShelves ??= new Repository<WarehouseShelf>(_context);
    public IRepository<WarehouseLocation> WarehouseLocations => _warehouseLocations ??= new Repository<WarehouseLocation>(_context);
    public IRepository<ComponentCategory> ComponentCategories => _componentCategories ??= new Repository<ComponentCategory>(_context);
    public IComponentRepository Components => _components ??= new ComponentRepository(_context);
    public IRepository<ComponentType> ComponentTypes => _componentTypes ??= new Repository<ComponentType>(_context);
    public IRepository<Supplier> Suppliers => _suppliers ??= new Repository<Supplier>(_context);
    public IStockLocationRepository StockLocations => _stockLocations ??= new StockLocationRepository(_context);
    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(_context);
    public IRepository<RefreshToken> RefreshTokens => _refreshTokens ??= new Repository<RefreshToken>(_context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
