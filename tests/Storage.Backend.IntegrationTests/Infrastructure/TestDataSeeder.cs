using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Storage.Data;

namespace Storage.Backend.IntegrationTests.Infrastructure;

public static class TestDataSeeder
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "AdminPass123!";

    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync(cancellationToken);

        if (!await context.Users.AnyAsync(cancellationToken))
        {
            var now = DateTime.UtcNow;
            var systemUserId = Guid.NewGuid();

            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin_test",
                Email = AdminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword, workFactor: 12),
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
                Email = "user@test.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("UserPass123!", workFactor: 12),
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
                Email = "readonly@test.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("ReadOnlyPass123!", workFactor: 12),
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

            await context.WarehouseLocations.AddAsync(storageLocation, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

    }
}
