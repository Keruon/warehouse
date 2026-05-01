using Microsoft.EntityFrameworkCore;

namespace Storage.Data.Repositories;

public class ComponentRepository : Repository<Component>, IComponentRepository
{
    public ComponentRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<Component>> GetByTypeAsync(Guid componentTypeId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.ComponentTypeId == componentTypeId)
            .OrderBy(x => x.PartNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Component>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.SupplierId == supplierId)
            .OrderBy(x => x.PartNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Component>> SearchAsync(
        string? name,
        string? partNumber,
        string? manufacturer,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(x => x.ComponentTypeName != null && EF.Functions.ILike(x.ComponentTypeName, $"%{name}%"));
        }

        if (!string.IsNullOrWhiteSpace(partNumber))
        {
            query = query.Where(x => EF.Functions.ILike(x.PartNumber, $"%{partNumber}%"));
        }

        if (!string.IsNullOrWhiteSpace(manufacturer))
        {
            query = query.Where(x => x.SupplierName != null && EF.Functions.ILike(x.SupplierName, $"%{manufacturer}%"));
        }

        return await query
            .OrderBy(x => x.PartNumber)
            .ToListAsync(cancellationToken);
    }
}
