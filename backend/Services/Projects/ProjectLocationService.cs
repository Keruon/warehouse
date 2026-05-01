using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Services.Projects;

public sealed class ProjectLocationService : IProjectLocationService
{
    private const string DefaultShelfName = "default";
    private readonly ApplicationDbContext _context;

    public ProjectLocationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> ResolveShelfIdAsync(LocationKind locationKind, Guid? requestedShelfId, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (locationKind != LocationKind.Project)
        {
            return requestedShelfId ?? throw new KeyNotFoundException("Shelf was not found.");
        }

        var productionArea = await GetProductionAreaAsync(cancellationToken);

        var defaultShelf = await _context.WarehouseShelves
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.AreaId == productionArea.Id && x.Name == DefaultShelfName && x.Code == DefaultShelfName,
                cancellationToken);

        if (defaultShelf is null)
        {
            defaultShelf = new WarehouseShelf
            {
                Id = Guid.NewGuid(),
                AreaId = productionArea.Id,
                Name = DefaultShelfName,
                Code = DefaultShelfName,
                Description = "Default shelf for production project locations.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                CreatedBy = actorId,
                ModifiedBy = actorId
            };

            await _context.WarehouseShelves.AddAsync(defaultShelf, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else if (!defaultShelf.IsActive)
        {
            defaultShelf.IsActive = true;
            defaultShelf.ModifiedAt = DateTime.UtcNow;
            defaultShelf.ModifiedBy = actorId;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return defaultShelf.Id;
    }

    public async Task EnsureShelfSupportsProjectLocationAsync(Guid shelfId, CancellationToken cancellationToken = default)
    {
        var isProductionShelf = await _context.WarehouseShelves
            .IgnoreQueryFilters()
            .Where(x => x.Id == shelfId)
            .Join(
                _context.WarehouseAreas.IgnoreQueryFilters(),
                shelf => shelf.AreaId,
                area => area.Id,
                (shelf, area) => area.ZoneType)
            .AnyAsync(zoneType => zoneType == ZoneType.Production, cancellationToken);

        if (!isProductionShelf)
        {
            throw new InvalidOperationException("Project locations can only be created inside the production zone.");
        }
    }

    public async Task<WarehouseLocation> GetProjectLocationAsync(Guid locationId, bool requireActive, CancellationToken cancellationToken = default)
    {
        var location = await _context.WarehouseLocations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == locationId, cancellationToken)
            ?? throw new KeyNotFoundException("Project location was not found.");

        if (location.LocationKind != LocationKind.Project)
        {
            throw new InvalidOperationException("Selected location is not a project location.");
        }

        if (requireActive && !location.IsActive)
        {
            throw new InvalidOperationException("Project location is inactive.");
        }

        await EnsureShelfSupportsProjectLocationAsync(location.ShelfId, cancellationToken);
        return location;
    }

    public async Task<IReadOnlyList<ProjectLocationSummaryResponse>> ListProjectsAsync(Guid? currentUserId, CancellationToken cancellationToken = default)
    {
        var activeProjectId = currentUserId.HasValue
            ? await _context.Users
                .AsNoTracking()
                .Where(x => x.Id == currentUserId.Value)
                .Select(x => x.ActiveProjectLocationId)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        return await _context.WarehouseLocations
            .AsNoTracking()
            .Where(x => x.LocationKind == LocationKind.Project)
            .Join(
                _context.WarehouseShelves.IgnoreQueryFilters().AsNoTracking(),
                location => location.ShelfId,
                shelf => shelf.Id,
                (location, shelf) => new { location, shelf })
            .Where(x => _context.WarehouseAreas.IgnoreQueryFilters().Any(area => area.Id == x.shelf.AreaId && area.ZoneType == ZoneType.Production))
            .OrderBy(x => x.location.Name)
            .Select(x => new ProjectLocationSummaryResponse
            {
                Id = x.location.Id,
                ShelfId = x.location.ShelfId,
                AreaId = x.shelf.AreaId,
                Name = x.location.Name,
                Code = x.location.Code,
                IsActive = x.location.IsActive,
                IsCurrentActiveProject = activeProjectId.HasValue && x.location.Id == activeProjectId.Value
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectLocationSummaryResponse?> GetActiveProjectAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(x => x.Id == userId && x.ActiveProjectLocationId != null)
            .Join(
                _context.WarehouseLocations.AsNoTracking(),
                user => user.ActiveProjectLocationId,
                location => (Guid?)location.Id,
                (user, location) => location)
            .Join(
                _context.WarehouseShelves.IgnoreQueryFilters().AsNoTracking(),
                location => location.ShelfId,
                shelf => shelf.Id,
                (location, shelf) => new ProjectLocationSummaryResponse
                {
                    Id = location.Id,
                    ShelfId = location.ShelfId,
                    AreaId = shelf.AreaId,
                    Name = location.Name,
                    Code = location.Code,
                    IsActive = location.IsActive,
                    IsCurrentActiveProject = true
                })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProjectLocationSummaryResponse> SetActiveProjectAsync(Guid userId, Guid locationId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");

        var project = await GetProjectLocationAsync(locationId, requireActive: true, cancellationToken);

        user.ActiveProjectLocationId = project.Id;
        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = userId;
        await _context.SaveChangesAsync(cancellationToken);

        return await GetActiveProjectAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Active project could not be resolved after update.");
    }

    public async Task ClearActiveProjectAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");

        user.ActiveProjectLocationId = null;
        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = userId;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<WarehouseArea> GetProductionAreaAsync(CancellationToken cancellationToken)
    {
        var areas = await _context.WarehouseAreas
            .IgnoreQueryFilters()
            .Where(x => x.ZoneType == ZoneType.Production)
            .ToListAsync(cancellationToken);

        if (areas.Count == 0)
        {
            throw new InvalidOperationException("Production area was not found.");
        }

        if (areas.Count > 1)
        {
            throw new InvalidOperationException("Expected a single production area, but found multiple.");
        }

        return areas[0];
    }

        public async Task<ProjectLocationSummaryResponse> CreateProjectAsync(string name, string code, Guid actorId, CancellationToken cancellationToken = default)
        {
            name = name?.Trim() ?? string.Empty;
            code = code?.Trim().ToUpperInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Name and code are required.");

            var duplicate = await _context.WarehouseLocations
                .IgnoreQueryFilters()
                .AnyAsync(x => x.LocationKind == LocationKind.Project && x.Code.ToUpper() == code, cancellationToken);
            if (duplicate)
                throw new InvalidOperationException("duplicate_project_code");

            var shelfId = await ResolveShelfIdAsync(LocationKind.Project, null, actorId, cancellationToken);
            var now = DateTime.UtcNow;
            var location = new WarehouseLocation
            {
                Id = Guid.NewGuid(),
                ShelfId = shelfId,
                LocationKind = LocationKind.Project,
                Name = name,
                Code = code,
                IsActive = true,
                BinX = 0,
                BinY = 0,
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = actorId,
                ModifiedBy = actorId
            };
            await _context.WarehouseLocations.AddAsync(location, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // ProjectLocationSummaryResponse projection
            var shelf = await _context.WarehouseShelves.AsNoTracking().FirstAsync(x => x.Id == shelfId, cancellationToken);
            var areaId = shelf.AreaId;
            return new ProjectLocationSummaryResponse
            {
                Id = location.Id,
                ShelfId = location.ShelfId,
                AreaId = areaId,
                Name = location.Name,
                Code = location.Code,
                IsActive = location.IsActive,
                IsCurrentActiveProject = false
            };
        }

        public async Task<ProjectLocationSummaryResponse> DeactivateProjectAsync(Guid locationId, Guid actorId, CancellationToken cancellationToken = default)
        {
            var location = await _context.WarehouseLocations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == locationId, cancellationToken)
                ?? throw new KeyNotFoundException("Project location was not found.");
            if (location.LocationKind != LocationKind.Project)
                throw new InvalidOperationException("Selected location is not a project location.");
            if (!location.IsActive)
            {
                // Already inactive, return projection
                var shelf = await _context.WarehouseShelves.AsNoTracking().FirstAsync(x => x.Id == location.ShelfId, cancellationToken);
                return new ProjectLocationSummaryResponse
                {
                    Id = location.Id,
                    ShelfId = location.ShelfId,
                    AreaId = shelf.AreaId,
                    Name = location.Name,
                    Code = location.Code,
                    IsActive = location.IsActive,
                    IsCurrentActiveProject = false
                };
            }
            location.IsActive = false;
            location.ModifiedAt = DateTime.UtcNow;
            location.ModifiedBy = actorId;
            // Bulk clear active project for all users
            var users = _context.Users.Where(u => u.ActiveProjectLocationId == locationId);
            await users.ForEachAsync(u => { u.ActiveProjectLocationId = null; }, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            var shelf2 = await _context.WarehouseShelves.AsNoTracking().FirstAsync(x => x.Id == location.ShelfId, cancellationToken);
            return new ProjectLocationSummaryResponse
            {
                Id = location.Id,
                ShelfId = location.ShelfId,
                AreaId = shelf2.AreaId,
                Name = location.Name,
                Code = location.Code,
                IsActive = location.IsActive,
                IsCurrentActiveProject = false
            };
        }

        public async Task<ProjectLocationSummaryResponse> ActivateProjectAsync(Guid locationId, Guid actorId, CancellationToken cancellationToken = default)
        {
            var location = await _context.WarehouseLocations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == locationId, cancellationToken)
                ?? throw new KeyNotFoundException("Project location was not found.");
            if (location.LocationKind != LocationKind.Project)
                throw new InvalidOperationException("Selected location is not a project location.");
            if (location.IsActive)
            {
                var shelf = await _context.WarehouseShelves.AsNoTracking().FirstAsync(x => x.Id == location.ShelfId, cancellationToken);
                return new ProjectLocationSummaryResponse
                {
                    Id = location.Id,
                    ShelfId = location.ShelfId,
                    AreaId = shelf.AreaId,
                    Name = location.Name,
                    Code = location.Code,
                    IsActive = location.IsActive,
                    IsCurrentActiveProject = false
                };
            }
            location.IsActive = true;
            location.ModifiedAt = DateTime.UtcNow;
            location.ModifiedBy = actorId;
            await _context.SaveChangesAsync(cancellationToken);
            var shelf2 = await _context.WarehouseShelves.AsNoTracking().FirstAsync(x => x.Id == location.ShelfId, cancellationToken);
            return new ProjectLocationSummaryResponse
            {
                Id = location.Id,
                ShelfId = location.ShelfId,
                AreaId = shelf2.AreaId,
                Name = location.Name,
                Code = location.Code,
                IsActive = location.IsActive,
                IsCurrentActiveProject = false
            };
        }

        public async Task DeleteProjectAsync(Guid locationId, CancellationToken cancellationToken = default)
        {
            var location = await _context.WarehouseLocations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == locationId, cancellationToken)
                ?? throw new KeyNotFoundException("Project location was not found.");
            if (location.LocationKind != LocationKind.Project)
                throw new InvalidOperationException("Selected location is not a project location.");
            // Stock guard
            var hasStock = await _context.StockLocations.AnyAsync(x => x.LocationId == locationId && x.Quantity > 0, cancellationToken);
            if (hasStock)
                throw new InvalidOperationException("project_has_stock");
            // Clear active project for all users
            var users = _context.Users.Where(u => u.ActiveProjectLocationId == locationId);
            await users.ForEachAsync(u => { u.ActiveProjectLocationId = null; }, cancellationToken);
            _context.WarehouseLocations.Remove(location);
            await _context.SaveChangesAsync(cancellationToken);
        }
}