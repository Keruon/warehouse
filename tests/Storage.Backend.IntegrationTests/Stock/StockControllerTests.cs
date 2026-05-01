using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Storage.Backend.IntegrationTests.Infrastructure;
using Storage.Data;

namespace Storage.Backend.IntegrationTests.Stock;

[Collection(IntegrationCollection.Name)]
public sealed class StockControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public StockControllerTests(PostgreSqlContainerFixture postgresFixture)
    {
        _factory = new CustomWebApplicationFactory(postgresFixture);
    }

    [Fact]
    public async Task ReceiveStock_WhenRequestIsValid_ShouldPersistStock()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);
        var baseline = await LoadInventoryBaselineAsync();

        var response = await client.PostAsJsonAsync("/api/stock/receive", new
        {
            componentId = baseline.ComponentId,
            locationId = baseline.LocationOneId,
            quantity = 5,
            batchCode = "BATCH-1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var levels = await client.GetFromJsonAsync<JsonElement>($"/api/stock/component/{baseline.ComponentId}");
        levels.ValueKind.Should().Be(JsonValueKind.Array);
        levels.EnumerateArray().Any(x => x.GetProperty("locationId").GetGuid() == baseline.LocationOneId && x.GetProperty("quantity").GetInt32() == 5).Should().BeTrue();
    }

    [Fact]
    public async Task ReceiveStock_WhenQuantityIsZero_ShouldReturnBadRequest()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);
        var baseline = await LoadInventoryBaselineAsync();

        var response = await client.PostAsJsonAsync("/api/stock/receive", new
        {
            componentId = baseline.ComponentId,
            locationId = baseline.LocationOneId,
            quantity = 0
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TransferStock_WhenStockExists_ShouldMoveQuantityToTargetLocation()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);
        var baseline = await LoadInventoryBaselineAsync();

        (await client.PostAsJsonAsync("/api/stock/receive", new
        {
            componentId = baseline.ComponentId,
            locationId = baseline.LocationOneId,
            quantity = 8,
            batchCode = "MOVE-1"
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        var transfer = await client.PostAsJsonAsync("/api/stock/transfer", new
        {
            componentId = baseline.ComponentId,
            fromLocationId = baseline.LocationOneId,
            toLocationId = baseline.LocationTwoId,
            quantity = 3
        });

        transfer.StatusCode.Should().Be(HttpStatusCode.OK);

        var sourceInventory = await client.GetFromJsonAsync<JsonElement>($"/api/stock/location/{baseline.LocationOneId}");
        var targetInventory = await client.GetFromJsonAsync<JsonElement>($"/api/stock/location/{baseline.LocationTwoId}");

        sourceInventory.EnumerateArray().Single(x => x.GetProperty("componentId").GetGuid() == baseline.ComponentId).GetProperty("quantity").GetInt32().Should().Be(5);
        targetInventory.EnumerateArray().Single(x => x.GetProperty("componentId").GetGuid() == baseline.ComponentId).GetProperty("quantity").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task TransferStock_WhenSourceAndTargetMatch_ShouldReturnBadRequest()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);
        var baseline = await LoadInventoryBaselineAsync();

        var response = await client.PostAsJsonAsync("/api/stock/transfer", new
        {
            componentId = baseline.ComponentId,
            fromLocationId = baseline.LocationOneId,
            toLocationId = baseline.LocationOneId,
            quantity = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GatherStock_WhenActiveProjectExists_ShouldMoveStockToProject()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);
        var baseline = await LoadInventoryBaselineAsync();

        (await client.PostAsJsonAsync("/api/stock/receive", new
        {
            componentId = baseline.ComponentId,
            locationId = baseline.LocationOneId,
            quantity = 4,
            batchCode = "GATHER-1"
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        var projectResponse = await client.PostAsJsonAsync("/api/projects", new { name = "Gather Project", code = "GATHER-PROJ" });
        projectResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var document = await JsonDocument.ParseAsync(await projectResponse.Content.ReadAsStreamAsync());
        var projectId = document.RootElement.GetProperty("id").GetGuid();

        (await client.PutAsync($"/api/projects/active/{projectId}", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var gather = await client.PostAsJsonAsync("/api/stock/gather", new
        {
            componentId = baseline.ComponentId,
            locationId = baseline.LocationOneId,
            quantity = 2
        });

        gather.StatusCode.Should().Be(HttpStatusCode.OK);
        using var gatherDocument = await JsonDocument.ParseAsync(await gather.Content.ReadAsStreamAsync());
        gatherDocument.RootElement.GetProperty("locationId").GetGuid().Should().Be(projectId);
        gatherDocument.RootElement.GetProperty("locationKind").GetString().Should().Be("Project");

        var projectInventory = await client.GetFromJsonAsync<JsonElement>($"/api/stock/location/{projectId}");
        projectInventory.EnumerateArray().Single(x => x.GetProperty("componentId").GetGuid() == baseline.ComponentId).GetProperty("quantity").GetInt32().Should().Be(2);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    private async Task<(Guid ComponentId, Guid LocationOneId, Guid LocationTwoId)> LoadInventoryBaselineAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var componentId = await context.Components.Select(x => x.Id).FirstAsync();
        var locations = await context.WarehouseLocations
            .Where(x => x.LocationKind == LocationKind.Warehouse)
            .OrderBy(x => x.Code)
            .Select(x => x.Id)
            .Take(2)
            .ToListAsync();

        return (componentId, locations[0], locations[1]);
    }
}
