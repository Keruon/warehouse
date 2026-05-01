namespace Storage.Data.Repositories;

public interface IComponentRepository : IRepository<Component>
{
    Task<IReadOnlyList<Component>> GetByTypeAsync(Guid componentTypeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Component>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Component>> SearchAsync(
        string? name,
        string? partNumber,
        string? manufacturer,
        CancellationToken cancellationToken = default);
}
