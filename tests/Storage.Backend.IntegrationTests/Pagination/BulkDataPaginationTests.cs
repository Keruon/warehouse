using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Storage.Backend.IntegrationTests.Infrastructure;
using Storage.Data;

namespace Storage.Backend.IntegrationTests.Pagination;

/// <summary>
/// Integration tests for bulk data pagination across all primary API endpoints.
/// Verifies that the backend can efficiently handle and paginate large datasets (2,048+ records).
/// </summary>
[Collection(IntegrationCollection.Name)]
public sealed class BulkDataPaginationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public BulkDataPaginationTests(PostgreSqlContainerFixture postgresFixture)
    {
        _factory = new CustomWebApplicationFactory(postgresFixture);
    }

    /// <summary>
    /// Creates 2,048+ records for a given endpoint, distributed across shelves where applicable.
    /// </summary>
    private async Task SeedLargeDatasetAsync(IServiceProvider serviceProvider, string endpoint, string dataType)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var actorId = await context.Users.Select(x => x.Id).FirstAsync();

        switch (dataType)
        {
            case "suppliers":
                await SeedSuppliersAsync(context, actorId, now);
                break;

            case "categories":
                await SeedCategoriesAsync(context, actorId, now);
                break;

            case "types":
                var categoryIds = await context.ComponentCategories
                    .Where(c => c.IsActive)
                    .Select(c => c.Id)
                    .ToListAsync();
                await SeedComponentTypesAsync(context, categoryIds, actorId, now);
                break;

            case "components":
                var typeIds = await context.ComponentTypes
                    .Where(c => c.IsActive)
                    .Select(c => c.Id)
                    .ToListAsync();
                var supplierIds = await context.Suppliers
                    .Where(s => s.IsActive)
                    .Select(s => s.Id)
                    .ToListAsync();
                await SeedComponentsAsync(context, typeIds, supplierIds, actorId, now);
                break;

            case "areas":
                await SeedAreasAsync(context, actorId, now);
                break;

            case "shelves":
                var areaIds = await context.WarehouseAreas
                    .Where(a => a.IsActive)
                    .Select(a => a.Id)
                    .ToListAsync();
                await SeedShelvesAsync(context, areaIds, actorId, now);
                break;

            case "locations":
                var shelfIds = await context.WarehouseShelves
                    .Where(s => s.IsActive)
                    .Select(s => s.Id)
                    .ToListAsync();
                await SeedLocationsAsync(context, shelfIds, actorId, now);
                break;
        }
    }

    private static async Task SeedSuppliersAsync(ApplicationDbContext context, Guid actorId, DateTime now)
    {
        var suppliers = new List<Supplier>();
        for (int i = 0; i < 2048; i++)
        {
            suppliers.Add(new Supplier
            {
                Id = Guid.NewGuid(),
                Code = $"SUP-{i:D5}",
                Name = $"Test Supplier {i}",
                ContactEmail = $"supplier{i}@test.local",
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = actorId,
                ModifiedBy = actorId
            });

            if (suppliers.Count >= 500)
            {
                await context.Suppliers.AddRangeAsync(suppliers);
                await context.SaveChangesAsync();
                suppliers.Clear();
            }
        }

        if (suppliers.Any())
        {
            await context.Suppliers.AddRangeAsync(suppliers);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context, Guid actorId, DateTime now)
    {
        // Create 40 root categories + 1,000+ children
        var categories = new List<ComponentCategory>();

        // Root categories
        for (int i = 0; i < 40; i++)
        {
            categories.Add(new ComponentCategory
            {
                Id = Guid.NewGuid(),
                Name = $"Category Root {i}",
                ParentId = null,
                Description = $"Root category {i}",
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = actorId,
                ModifiedBy = actorId
            });
        }

        await context.ComponentCategories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        // Child categories
        var rootIds = await context.ComponentCategories
            .Where(c => c.ParentId == null && c.IsActive)
            .Select(c => c.Id)
            .ToListAsync();

        var childCategories = new List<ComponentCategory>();
        int childIndex = 0;

        foreach (var rootId in rootIds.Repeat(26)) // 26 children per root = 1,040 total
        {
            childCategories.Add(new ComponentCategory
            {
                Id = Guid.NewGuid(),
                Name = $"Subcategory {childIndex}",
                ParentId = rootId,
                Description = $"Subcategory of parent",
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = actorId,
                ModifiedBy = actorId
            });

            childIndex++;

            if (childCategories.Count >= 500)
            {
                await context.ComponentCategories.AddRangeAsync(childCategories);
                await context.SaveChangesAsync();
                childCategories.Clear();
            }
        }

        if (childCategories.Any())
        {
            await context.ComponentCategories.AddRangeAsync(childCategories);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedComponentTypesAsync(ApplicationDbContext context, List<Guid> categoryIds, Guid actorId, DateTime now)
    {
        if (!categoryIds.Any()) throw new InvalidOperationException("No categories available for component types");

        var types = new List<ComponentType>();
        var categoryIndex = 0;

        for (int i = 0; i < 2048; i++)
        {
            var categoryId = categoryIds[categoryIndex % categoryIds.Count];
            categoryIndex++;

            types.Add(new ComponentType
            {
                Id = Guid.NewGuid(),
                CategoryId = categoryId,
                Kind = $"Kind-{i % 10}",
                Value = $"Value-{i}",
                Footprint = $"Footprint-{i % 50}",
                Type = i % 2 == 0 ? "SMD" : "DIP",
                Description = $"Component Type {i}",
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = actorId,
                ModifiedBy = actorId
            });

            if (types.Count >= 500)
            {
                await context.ComponentTypes.AddRangeAsync(types);
                await context.SaveChangesAsync();
                types.Clear();
            }
        }

        if (types.Any())
        {
            await context.ComponentTypes.AddRangeAsync(types);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedComponentsAsync(ApplicationDbContext context, List<Guid> typeIds, List<Guid> supplierIds, Guid actorId, DateTime now)
    {
        if (!typeIds.Any()) throw new InvalidOperationException("No component types available");
        if (!supplierIds.Any()) throw new InvalidOperationException("No suppliers available");

        var components = new List<Component>();
        var typeIndex = 0;
        var supplierIndex = 0;

        for (int i = 0; i < 2048; i++)
        {
            var typeId = typeIds[typeIndex % typeIds.Count];
            var supplierId = supplierIds[supplierIndex % supplierIds.Count];

            typeIndex++;
            if (typeIndex % 5 == 0) supplierIndex++;

            components.Add(new Component
            {
                Id = Guid.NewGuid(),
                ComponentTypeId = typeId,
                PartNumber = $"PART-{i:D5}",
                BatchCode = $"BATCH-{i % 100}",
                SupplierId = supplierId,
                SupplierPartNumber = $"SUP-PART-{i}",
                UnitCost = 0.50m + (i % 100) * 0.01m,
                MinimumStockLevel = 5 + (i % 50),
                MaximumStockLevel = 500 + (i % 1000),
                ReorderPoint = 25 + (i % 50),
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = actorId,
                ModifiedBy = actorId
            });

            if (components.Count >= 500)
            {
                await context.Components.AddRangeAsync(components);
                await context.SaveChangesAsync();
                components.Clear();
            }
        }

        if (components.Any())
        {
            await context.Components.AddRangeAsync(components);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedAreasAsync(ApplicationDbContext context, Guid actorId, DateTime now)
    {
        var areas = new List<WarehouseArea>();

        for (int i = 0; i < 2048; i++)
        {
            var zoneType = i % 4 switch
            {
                0 => ZoneType.Storage,
                1 => ZoneType.Production,
                2 => ZoneType.QualityControl,
                _ => ZoneType.Returns
            };

            areas.Add(new WarehouseArea
            {
                Id = Guid.NewGuid(),
                Name = $"Area-{i:D4}",
                Code = $"A{i:D4}",
                ZoneType = zoneType,
                FloorLevel = (i % 5) + 1,
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = actorId,
                ModifiedBy = actorId
            });

            if (areas.Count >= 500)
            {
                await context.WarehouseAreas.AddRangeAsync(areas);
                await context.SaveChangesAsync();
                areas.Clear();
            }
        }

        if (areas.Any())
        {
            await context.WarehouseAreas.AddRangeAsync(areas);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedShelvesAsync(ApplicationDbContext context, List<Guid> areaIds, Guid actorId, DateTime now)
    {
        var shelves = new List<WarehouseShelf>();
        var areaIndex = 0;

        for (int i = 0; i < 2048; i++)
        {
            var areaId = areaIds[areaIndex % areaIds.Count];
            areaIndex++;

            shelves.Add(new WarehouseShelf
            {
                Id = Guid.NewGuid(),
                AreaId = areaId,
                Name = $"Shelf-{i:D4}",
                Code = $"S{i:D4}",
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = actorId,
                ModifiedBy = actorId
            });

            if (shelves.Count >= 500)
            {
                await context.WarehouseShelves.AddRangeAsync(shelves);
                await context.SaveChangesAsync();
                shelves.Clear();
            }
        }

        if (shelves.Any())
        {
            await context.WarehouseShelves.AddRangeAsync(shelves);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedLocationsAsync(ApplicationDbContext context, List<Guid> shelfIds, Guid actorId, DateTime now)
    {
        var locations = new List<WarehouseLocation>();
        var shelfIndex = 0;
        int binX = 0;
        int binY = 0;

        for (int i = 0; i < 2048; i++)
        {
            var shelfId = shelfIds[shelfIndex % shelfIds.Count];

            locations.Add(new WarehouseLocation
            {
                Id = Guid.NewGuid(),
                ShelfId = shelfId,
                LocationKind = i % 2 == 0 ? LocationKind.Warehouse : LocationKind.Staging,
                Name = $"Loc-{i:D4}",
                Code = $"L{i:D4}",
                BinX = binX,
                BinY = binY,
                IsReserved = false,
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
                CreatedBy = actorId,
                ModifiedBy = actorId
            });

            // Cycle through bin coordinates: 0-31 x 0-7 = 256 combinations per shelf
            binY++;
            if (binY >= 8)
            {
                binY = 0;
                binX++;
            }

            if (binX >= 32)
            {
                binX = 0;
                shelfIndex++;
            }

            if (locations.Count >= 500)
            {
                await context.WarehouseLocations.AddRangeAsync(locations);
                await context.SaveChangesAsync();
                locations.Clear();
            }
        }

        if (locations.Any())
        {
            await context.WarehouseLocations.AddRangeAsync(locations);
            await context.SaveChangesAsync();
        }
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    private static async Task<JsonDocument> ParseJsonAsync(HttpResponseMessage response)
    {
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    }

    #region Supplier Pagination Tests

    [Fact]
    public async Task SupplierPagination_WhenFetching2048Records_ShouldReturnCorrectTotalCount()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        await SeedLargeDatasetAsync(_factory.Services, "suppliers", "suppliers");

        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/suppliers?pageSize=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = await ParseJsonAsync(response);
        var totalItems = document.RootElement.GetProperty("totalItems").GetInt32();

        totalItems.Should().Be(2048);
    }

    [Fact]
    public async Task SupplierPagination_WhenPageSizeIs512_ShouldReturnExactly512ItemsPerPage()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        await SeedLargeDatasetAsync(_factory.Services, "suppliers", "suppliers");

        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/suppliers?page=1&pageSize=512");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = await ParseJsonAsync(response);
        var items = document.RootElement.GetProperty("items");
        items.GetArrayLength().Should().Be(512);
    }

    [Fact]
    public async Task SupplierPagination_WhenTraversingAllPages_ShouldHaveNoGaps()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        await SeedLargeDatasetAsync(_factory.Services, "suppliers", "suppliers");

        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var collectedIds = new HashSet<Guid>();
        int pageSize = 512;
        int totalPages = 4; // 2048 / 512 = 4 full pages

        for (int page = 1; page <= totalPages; page++)
        {
            var response = await client.GetAsync($"/api/suppliers?page={page}&pageSize={pageSize}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var document = await ParseJsonAsync(response);
            var items = document.RootElement.GetProperty("items");

            foreach (var item in items.EnumerateArray())
            {
                var id = item.GetProperty("id").GetGuid();
                collectedIds.Add(id);
            }
        }

        collectedIds.Should().HaveCount(2048, "all records should be uniquely retrieved across pages");
    }

    #endregion

    #region Category Pagination Tests

    [Fact]
    public async Task CategoryPagination_WhenFetching1040PlusRecords_ShouldReturnCorrectTotalCount()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        await SeedLargeDatasetAsync(_factory.Services, "categories", "categories");

        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/component-categories?pageSize=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = await ParseJsonAsync(response);
        var totalItems = document.RootElement.GetProperty("totalItems").GetInt32();

        totalItems.Should().BeGreaterThanOrEqualTo(1040);
    }

    [Fact]
    public async Task CategoryPagination_WhenFilteringByParentId_ShouldReturnOnlyDirectChildren()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        await SeedLargeDatasetAsync(_factory.Services, "categories", "categories");

        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        // Get a root category
        var rootResponse = await client.GetAsync("/api/component-categories?parentId=&pageSize=1");
        rootResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var rootDocument = await ParseJsonAsync(rootResponse);
        var rootItems = rootDocument.RootElement.GetProperty("items");
        var firstRoot = rootItems[0];
        var rootId = firstRoot.GetProperty("id").GetString();

        // Get children of that root
        var childResponse = await client.GetAsync($"/api/component-categories?parentId={rootId}&pageSize=100");
        childResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var childDocument = await ParseJsonAsync(childResponse);
        var childItems = childDocument.RootElement.GetProperty("items");

        foreach (var child in childItems.EnumerateArray())
        {
            var childParentId = child.GetProperty("parentId").GetString();
            childParentId.Should().Be(rootId);
        }
    }

    #endregion

    #region Component Type Pagination Tests

    [Fact]
    public async Task ComponentTypePagination_WhenFetching2048Records_ShouldReturnCorrectTotalCount()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        await SeedCategoriesAsync(_factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>(),
            await GetActorIdAsync(), DateTime.UtcNow);
        await SeedLargeDatasetAsync(_factory.Services, "types", "types");

        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/component-types?pageSize=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = await ParseJsonAsync(response);
        var totalItems = document.RootElement.GetProperty("totalItems").GetInt32();

        totalItems.Should().Be(2048);
    }

    #endregion

    #region Component Pagination Tests

    [Fact]
    public async Task ComponentPagination_WhenFetching2048Records_ShouldReturnCorrectTotalCount()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);

        // Setup: Create categories, types, suppliers
        var context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var actorId = await GetActorIdAsync();
        var now = DateTime.UtcNow;

        await SeedCategoriesAsync(context, actorId, now);
        await SeedComponentTypesAsync(context, 
            await context.ComponentCategories.Where(c => c.IsActive).Select(c => c.Id).ToListAsync(), 
            actorId, now);
        await SeedSuppliersAsync(context, actorId, now);
        await SeedLargeDatasetAsync(_factory.Services, "components", "components");

        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/components?pageSize=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = await ParseJsonAsync(response);
        var totalItems = document.RootElement.GetProperty("totalItems").GetInt32();

        totalItems.Should().Be(2048);
    }

    #endregion

    #region Area Pagination Tests

    [Fact]
    public async Task AreaPagination_WhenFetching2048Records_ShouldReturnCorrectTotalCount()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        await SeedLargeDatasetAsync(_factory.Services, "areas", "areas");

        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/warehouse-areas?pageSize=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = await ParseJsonAsync(response);
        var totalItems = document.RootElement.GetProperty("totalItems").GetInt32();

        totalItems.Should().Be(2048);
    }

    [Fact]
    public async Task AreaPagination_WhenFilteringByZoneType_ShouldReturnOnlyMatchingRecords()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        await SeedLargeDatasetAsync(_factory.Services, "areas", "areas");

        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/warehouse-areas?zoneType=Storage&pageSize=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = await ParseJsonAsync(response);
        var totalItems = document.RootElement.GetProperty("totalItems").GetInt32();

        // With 4 zone types evenly distributed, expect ~512 Storage areas
        totalItems.Should().BeGreaterThan(400).And.BeLessThan(700);
    }

    #endregion

    #region Shelf Pagination Tests

    [Fact]
    public async Task ShelfPagination_WhenFetching2048Records_ShouldReturnCorrectTotalCount()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        await SeedLargeDatasetAsync(_factory.Services, "areas", "areas");
        await SeedLargeDatasetAsync(_factory.Services, "shelves", "shelves");

        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/warehouse-shelves?pageSize=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = await ParseJsonAsync(response);
        var totalItems = document.RootElement.GetProperty("totalItems").GetInt32();

        totalItems.Should().Be(2048);
    }

    #endregion

    #region Location Pagination Tests

    [Fact]
    public async Task LocationPagination_WhenFetching2048Records_ShouldReturnCorrectTotalCount()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        await SeedLargeDatasetAsync(_factory.Services, "areas", "areas");
        await SeedLargeDatasetAsync(_factory.Services, "shelves", "shelves");
        await SeedLargeDatasetAsync(_factory.Services, "locations", "locations");

        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/warehouse-locations?pageSize=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = await ParseJsonAsync(response);
        var totalItems = document.RootElement.GetProperty("totalItems").GetInt32();

        totalItems.Should().Be(2048);
    }

    [Fact]
    public async Task LocationPagination_WhenTraversingAllPages_ShouldRetrieveAllWithoutDuplicates()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        await SeedLargeDatasetAsync(_factory.Services, "areas", "areas");
        await SeedLargeDatasetAsync(_factory.Services, "shelves", "shelves");
        await SeedLargeDatasetAsync(_factory.Services, "locations", "locations");

        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var collectedCodes = new HashSet<string>();
        int pageSize = 512;
        int totalPages = 4;

        for (int page = 1; page <= totalPages; page++)
        {
            var response = await client.GetAsync($"/api/warehouse-locations?page={page}&pageSize={pageSize}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var document = await ParseJsonAsync(response);
            var items = document.RootElement.GetProperty("items");

            foreach (var item in items.EnumerateArray())
            {
                var code = item.GetProperty("code").GetString() ?? string.Empty;
                collectedCodes.Add(code);
            }
        }

        collectedCodes.Should().HaveCount(2048, "all location codes should be unique across all pages");
    }

    #endregion

    #region Performance & Consistency Tests

    [Fact]
    public async Task BulkPagination_WhenReadingMultipleEndpoints_ShouldCompleteWithinReasonableTime()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        await SeedLargeDatasetAsync(_factory.Services, "suppliers", "suppliers");
        await SeedLargeDatasetAsync(_factory.Services, "categories", "categories");

        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var endpoints = new[] { "suppliers", "component-categories" };
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        foreach (var endpoint in endpoints)
        {
            for (int page = 1; page <= 4; page++)
            {
                var response = await client.GetAsync($"/api/{endpoint}?page={page}&pageSize=512");
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        stopwatch.Stop();

        // Should complete 8 requests in under 10 seconds
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000);
    }

    [Fact]
    public async Task BulkPagination_WhenFilteringLargeDataset_ShouldReturnCorrectSubset()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        await SeedLargeDatasetAsync(_factory.Services, "suppliers", "suppliers");

        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/suppliers?isActive=true&pageSize=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = await ParseJsonAsync(response);
        var totalItems = document.RootElement.GetProperty("totalItems").GetInt32();
        var items = document.RootElement.GetProperty("items");

        // All seeded items are active
        totalItems.Should().Be(2048);

        foreach (var item in items.EnumerateArray())
        {
            var isActive = item.GetProperty("isActive").GetBoolean();
            isActive.Should().BeTrue();
        }
    }

    #endregion

    #region Helper Methods

    private async Task<Guid> GetActorIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Users.Where(u => u.IsActive).Select(u => u.Id).FirstAsync();
    }

    #endregion
}
