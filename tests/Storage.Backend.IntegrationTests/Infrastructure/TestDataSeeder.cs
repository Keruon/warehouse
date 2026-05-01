using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Storage.Data;

namespace Storage.Backend.IntegrationTests.Infrastructure;

public static class TestDataSeeder
{
    private static readonly SemaphoreSlim SetupLock = new(1, 1);
    private static bool _isDatabasePrepared;
    private const int TestPasswordWorkFactor = 4;

    private static readonly string AdminPasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword, workFactor: TestPasswordWorkFactor);
    private static readonly string UserPasswordHash = BCrypt.Net.BCrypt.HashPassword(UserPassword, workFactor: TestPasswordWorkFactor);
    private static readonly string ReadOnlyPasswordHash = BCrypt.Net.BCrypt.HashPassword(ReadOnlyPassword, workFactor: TestPasswordWorkFactor);

    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "AdminPass123!";
    public const string UserEmail = "user@test.local";
    public const string UserPassword = "UserPass123!";
    public const string ReadOnlyEmail = "readonly@test.local";
    public const string ReadOnlyPassword = "ReadOnlyPass123!";

    public static Task ResetAndSeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        => ResetAndSeedAsync(serviceProvider, includeInventoryData: false, cancellationToken);

    public static async Task ResetAndSeedAsync(IServiceProvider serviceProvider, bool includeInventoryData, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await EnsureDatabasePreparedAsync(context, cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE \"AuditLogs\", \"RefreshTokens\", \"StockLocations\", \"Components\", \"ComponentTypes\", \"ComponentCategories\", \"Suppliers\", \"Users\", \"WarehouseLocations\", \"WarehouseShelves\", \"WarehouseAreas\" RESTART IDENTITY CASCADE;",
            cancellationToken);

        await SeedAsync(serviceProvider, includeInventoryData, cancellationToken);
    }

    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        => await SeedAsync(serviceProvider, includeInventoryData: false, cancellationToken);

    public static async Task SeedAsync(IServiceProvider serviceProvider, bool includeInventoryData, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await EnsureDatabasePreparedAsync(context, cancellationToken);

        if (!await context.Users.AnyAsync(cancellationToken))
        {
            var now = DateTime.UtcNow;
            var systemUserId = Guid.NewGuid();

            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin_test",
                Email = AdminEmail,
                PasswordHash = AdminPasswordHash,
                Role = UserRole.Admin,
                FirstName = "Admin",
                LastName = "Tester",
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = systemUserId,
                ModifiedBy = systemUserId,
                IsActive = true
            };

            var standardUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "user_test",
                Email = UserEmail,
                PasswordHash = UserPasswordHash,
                Role = UserRole.User,
                FirstName = "User",
                LastName = "Tester",
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = systemUserId,
                ModifiedBy = systemUserId,
                IsActive = true
            };

            var readonlyUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "readonly_test",
                Email = ReadOnlyEmail,
                PasswordHash = ReadOnlyPasswordHash,
                Role = UserRole.ReadOnly,
                FirstName = "Read",
                LastName = "Only",
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = systemUserId,
                ModifiedBy = systemUserId,
                IsActive = true
            };

            await context.Users.AddRangeAsync([adminUser, standardUser, readonlyUser], cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        if (!await context.WarehouseAreas.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            var now = DateTime.UtcNow;
            var actorId = await context.Users.Select(x => x.Id).FirstAsync(cancellationToken);

            var productionArea = new WarehouseArea
            {
                Id = Guid.NewGuid(),
                Name = "Production",
                Code = "PROD",
                ZoneType = ZoneType.Production,
                FloorLevel = 1,
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = actorId,
                ModifiedBy = actorId,
                IsActive = true
            };

            var storageArea = new WarehouseArea
            {
                Id = Guid.NewGuid(),
                Name = "Storage",
                Code = "STOR",
                ZoneType = ZoneType.Storage,
                FloorLevel = 1,
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = actorId,
                ModifiedBy = actorId,
                IsActive = true
            };

            await context.WarehouseAreas.AddRangeAsync([productionArea, storageArea], cancellationToken);

            var defaultShelf = new WarehouseShelf
            {
                Id = Guid.NewGuid(),
                AreaId = productionArea.Id,
                Name = "default",
                Code = "default",
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = actorId,
                ModifiedBy = actorId,
                IsActive = true
            };

            var storageShelf = new WarehouseShelf
            {
                Id = Guid.NewGuid(),
                AreaId = storageArea.Id,
                Name = "A1",
                Code = "A1",
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = actorId,
                ModifiedBy = actorId,
                IsActive = true
            };

            await context.WarehouseShelves.AddRangeAsync([defaultShelf, storageShelf], cancellationToken);

            var storageLocation = new WarehouseLocation
            {
                Id = Guid.NewGuid(),
                ShelfId = storageShelf.Id,
                LocationKind = LocationKind.Warehouse,
                Name = "A1-01",
                Code = "A1-01",
                BinX = 0,
                BinY = 0,
                IsReserved = false,
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = actorId,
                ModifiedBy = actorId,
                IsActive = true
            };

            var storageLocationTwo = new WarehouseLocation
            {
                Id = Guid.NewGuid(),
                ShelfId = storageShelf.Id,
                LocationKind = LocationKind.Warehouse,
                Name = "A1-02",
                Code = "A1-02",
                BinX = 1,
                BinY = 0,
                IsReserved = false,
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = actorId,
                ModifiedBy = actorId,
                IsActive = true
            };

            await context.WarehouseLocations.AddRangeAsync([storageLocation, storageLocationTwo], cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        if (!includeInventoryData)
        {
            return;
        }

        var seedNow = DateTime.UtcNow;
        var seedActorId = await context.Users.Select(x => x.Id).FirstAsync(cancellationToken);

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Smoke Supplier",
            Code = "SMOKE-SUP",
            CreatedAt = seedNow,
            ModifiedAt = seedNow,
            CreatedBy = seedActorId,
            ModifiedBy = seedActorId,
            IsActive = true
        };

        var category = new ComponentCategory
        {
            Id = Guid.NewGuid(),
            Name = "Smoke Category",
            Description = "Seed category for stock tests.",
            CreatedAt = seedNow,
            ModifiedAt = seedNow,
            CreatedBy = seedActorId,
            ModifiedBy = seedActorId,
            IsActive = true
        };

        await context.Suppliers.AddAsync(supplier, cancellationToken);
        await context.ComponentCategories.AddAsync(category, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var componentType = new ComponentType
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Kind = "Resistor",
            Value = "10k",
            Footprint = "0603",
            Type = ComponentPackageType.SMD,
            Description = "Seed component type for stock tests.",
            CreatedAt = seedNow,
            ModifiedAt = seedNow,
            CreatedBy = seedActorId,
            ModifiedBy = seedActorId,
            IsActive = true
        };

        await context.ComponentTypes.AddAsync(componentType, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var component = new Component
        {
            Id = Guid.NewGuid(),
            ComponentTypeId = componentType.Id,
            ComponentTypeName = "Resistor 10k 0603",
            PartNumber = "SMOKE-RES-10K-0603",
            UnitCost = 0.01m,
            SupplierId = supplier.Id,
            SupplierCode = supplier.Code,
            SupplierName = supplier.Name,
            SupplierPartNumber = "SUP-10K-0603",
            SupplierLeadTime = 7,
            QuantityOnHand = 0,
            QuantityReserved = 0,
            QuantityCommitted = 0,
            IsActive = true,
            CreatedAt = seedNow,
            ModifiedAt = seedNow,
            CreatedBy = seedActorId,
            ModifiedBy = seedActorId
        };

        await context.Components.AddAsync(component, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDatabasePreparedAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (_isDatabasePrepared)
        {
            return;
        }

        await SetupLock.WaitAsync(cancellationToken);
        try
        {
            if (_isDatabasePrepared)
            {
                return;
            }

            await context.Database.MigrateAsync(cancellationToken);
            await EnsureComponentTypeSchemaAsync(context, cancellationToken);
            _isDatabasePrepared = true;
        }
        finally
        {
            SetupLock.Release();
        }
    }

    private static async Task EnsureComponentTypeSchemaAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"ComponentTypes\" ALTER COLUMN \"Name\" SET DEFAULT '';",
            cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"ComponentTypes\" ADD COLUMN IF NOT EXISTS \"Kind\" text NOT NULL DEFAULT '';",
            cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"ComponentTypes\" ADD COLUMN IF NOT EXISTS \"Value\" text NOT NULL DEFAULT '';",
            cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"ComponentTypes\" ADD COLUMN IF NOT EXISTS \"Footprint\" text NULL;",
            cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "UPDATE \"ComponentTypes\" SET \"Kind\" = CASE WHEN COALESCE(TRIM(\"Kind\"), '') = '' THEN COALESCE(\"Name\", 'Legacy') ELSE \"Kind\" END, \"Value\" = CASE WHEN COALESCE(TRIM(\"Value\"), '') = '' THEN COALESCE(\"Name\", 'Unknown') ELSE \"Value\" END;",
            cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "UPDATE \"ComponentTypes\" SET \"Name\" = COALESCE(NULLIF(TRIM(\"Name\"), ''), CONCAT(COALESCE(NULLIF(TRIM(\"Kind\"), ''), 'Legacy'), CASE WHEN COALESCE(NULLIF(TRIM(\"Value\"), ''), '') = '' THEN '' ELSE ' ' || \"Value\" END, CASE WHEN COALESCE(NULLIF(TRIM(\"Footprint\"), ''), '') = '' THEN '' ELSE ' ' || \"Footprint\" END));",
            cancellationToken);
    }
}
