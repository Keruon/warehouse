using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Storage.Backend.IntegrationTests.Infrastructure;
using Storage.Data;

namespace Storage.Backend.IntegrationTests.Warehouse;

[Collection(IntegrationCollection.Name)]
public sealed class WarehouseCrudControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public WarehouseCrudControllerTests(PostgreSqlContainerFixture postgresFixture)
    {
        _factory = new CustomWebApplicationFactory(postgresFixture);
    }

    [Fact]
    public async Task AreaLifecycle_WhenAdminMutatesArea_ShouldCreateUpdateAndSoftDelete()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/areas", new
        {
            name = "Receiving",
            code = "RECV",
            zoneType = "Storage",
            floorLevel = 2,
            description = "Inbound area"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createDocument = await ParseJsonAsync(createResponse);
        var areaId = createDocument.RootElement.GetProperty("id").GetGuid();

        var updateResponse = await client.PutAsJsonAsync($"/api/areas/{areaId}", new
        {
            name = "Receiving Updated",
            code = "RECV",
            zoneType = "Storage",
            floorLevel = 2,
            description = "Inbound area updated",
            isActive = true
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var updateDocument = await ParseJsonAsync(updateResponse))
        {
            updateDocument.RootElement.GetProperty("name").GetString().Should().Be("Receiving Updated");
        }

        var deleteResponse = await client.DeleteAsync($"/api/areas/{areaId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getDeletedResponse = await client.GetAsync($"/api/areas/{areaId}");
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AreaCreate_WhenPayloadDuplicatesExistingArea_ShouldReturnConflict()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/areas", new
        {
            name = "Storage",
            code = "STOR",
            zoneType = "Storage",
            floorLevel = 1,
            description = "Duplicate"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ShelfLifecycle_WhenAdminMutatesShelf_ShouldCreateUpdateAndSoftDelete()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        var storageAreaId = await GetAreaIdByCodeAsync("STOR");
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/shelves", new
        {
            areaId = storageAreaId,
            name = "B1",
            code = "B1",
            weightLimitKg = 50,
            description = "Overflow shelf"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createDocument = await ParseJsonAsync(createResponse);
        var shelfId = createDocument.RootElement.GetProperty("id").GetGuid();

        var updateResponse = await client.PutAsJsonAsync($"/api/shelves/{shelfId}", new
        {
            areaId = storageAreaId,
            name = "B1-UPDATED",
            code = "B1",
            weightLimitKg = 75,
            description = "Overflow shelf updated",
            isActive = true
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await client.DeleteAsync($"/api/shelves/{shelfId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getDeletedResponse = await client.GetAsync($"/api/shelves/{shelfId}");
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShelfCreate_WhenAreaDoesNotExist_ShouldReturnNotFound()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/shelves", new
        {
            areaId = Guid.NewGuid(),
            name = "Ghost",
            code = "GHOST",
            weightLimitKg = 10,
            description = "Missing area"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LocationLifecycle_WhenAdminMutatesLocation_ShouldCreateUpdateAndSoftDelete()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        var storageShelfId = await GetShelfIdByCodeAsync("A1");
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/locations", new
        {
            shelfId = storageShelfId,
            locationKind = "Warehouse",
            name = "A1-03",
            code = "A1-03",
            description = "Top bin",
            binX = 2,
            binY = 0,
            depth = 10,
            width = 10,
            height = 10,
            volume = 1000,
            isReserved = false
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createDocument = await ParseJsonAsync(createResponse);
        var locationId = createDocument.RootElement.GetProperty("id").GetGuid();

        var updateResponse = await client.PutAsJsonAsync($"/api/locations/{locationId}", new
        {
            shelfId = storageShelfId,
            locationKind = "Warehouse",
            name = "A1-03-UPDATED",
            code = "A1-03",
            description = "Updated top bin",
            binX = 2,
            binY = 1,
            depth = 12,
            width = 10,
            height = 11,
            volume = 1320,
            isReserved = true,
            isActive = true
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await client.DeleteAsync($"/api/locations/{locationId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getDeletedResponse = await client.GetAsync($"/api/locations/{locationId}");
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LocationDelete_WhenLocationHasStock_ShouldReturnConflict()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        var (componentId, locationId) = await GetInventoryComponentAndLocationAsync();
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var receiveResponse = await client.PostAsJsonAsync("/api/stock/receive", new
        {
            componentId,
            locationId,
            quantity = 3,
            batchCode = "LOCK-1"
        });

        receiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await client.DeleteAsync($"/api/locations/{locationId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    private async Task<Guid> GetAreaIdByCodeAsync(string code)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.WarehouseAreas.IgnoreQueryFilters().Where(x => x.Code == code).Select(x => x.Id).FirstAsync();
    }

    private async Task<Guid> GetShelfIdByCodeAsync(string code)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.WarehouseShelves.IgnoreQueryFilters().Where(x => x.Code == code).Select(x => x.Id).FirstAsync();
    }

    private async Task<(Guid ComponentId, Guid LocationId)> GetInventoryComponentAndLocationAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var componentId = await context.Components.Select(x => x.Id).FirstAsync();
        var locationId = await context.WarehouseLocations
            .Where(x => x.LocationKind == LocationKind.Warehouse)
            .OrderBy(x => x.Code)
            .Select(x => x.Id)
            .FirstAsync();
        return (componentId, locationId);
    }

    private static async Task<JsonDocument> ParseJsonAsync(HttpResponseMessage response)
    {
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    }
}
