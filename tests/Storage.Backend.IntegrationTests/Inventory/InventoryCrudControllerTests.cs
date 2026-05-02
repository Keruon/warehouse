using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Storage.Backend.IntegrationTests.Infrastructure;
using Storage.Data;

namespace Storage.Backend.IntegrationTests.Inventory;

[Collection(IntegrationCollection.Name)]
public sealed class InventoryCrudControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public InventoryCrudControllerTests(PostgreSqlContainerFixture postgresFixture)
    {
        _factory = new CustomWebApplicationFactory(postgresFixture);
    }

    [Fact]
    public async Task SupplierLifecycle_WhenAdminMutatesSupplier_ShouldCreateUpdateAndSoftDelete()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/suppliers", new { code = "S-100", name = "Supplier 100" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createDocument = await ParseJsonAsync(createResponse);
        var supplierId = createDocument.RootElement.GetProperty("id").GetGuid();

        var updateResponse = await client.PutAsJsonAsync($"/api/suppliers/{supplierId}", new { code = "S-100", name = "Supplier 100 Updated", isActive = true });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await client.DeleteAsync($"/api/suppliers/{supplierId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getDeletedResponse = await client.GetAsync($"/api/suppliers/{supplierId}");
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SupplierCreate_WhenCodeAlreadyExists_ShouldReturnConflict()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/suppliers", new { code = "SMOKE-SUP", name = "Duplicate Supplier" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CategoryLifecycle_WhenAdminMutatesLeafCategory_ShouldCreateUpdateAndSoftDelete()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/component-categories", new
        {
            name = "Passives",
            parentId = (Guid?)null,
            description = "Passive parts"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createDocument = await ParseJsonAsync(createResponse);
        var categoryId = createDocument.RootElement.GetProperty("id").GetGuid();

        var updateResponse = await client.PutAsJsonAsync($"/api/component-categories/{categoryId}", new
        {
            name = "Passives Updated",
            parentId = (Guid?)null,
            description = "Passive parts updated",
            isActive = true
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await client.DeleteAsync($"/api/component-categories/{categoryId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getDeletedResponse = await client.GetAsync($"/api/component-categories/{categoryId}");
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CategoryDelete_WhenCategoryHasChildren_ShouldReturnConflict()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var parentResponse = await client.PostAsJsonAsync("/api/component-categories", new { name = "Parent", parentId = (Guid?)null, description = "Parent" });
        using var parentDocument = await ParseJsonAsync(parentResponse);
        var parentId = parentDocument.RootElement.GetProperty("id").GetGuid();

        var childResponse = await client.PostAsJsonAsync("/api/component-categories", new { name = "Child", parentId, description = "Child" });
        childResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var deleteResponse = await client.DeleteAsync($"/api/component-categories/{parentId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ComponentTypeLifecycle_WhenAdminMutatesUnusedType_ShouldCreateUpdateAndSoftDelete()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var categoryResponse = await client.PostAsJsonAsync("/api/component-categories", new { name = "Semiconductors", parentId = (Guid?)null, description = "Semis" });
        using var categoryDocument = await ParseJsonAsync(categoryResponse);
        var categoryId = categoryDocument.RootElement.GetProperty("id").GetGuid();

        var createResponse = await client.PostAsJsonAsync("/api/component-types", new
        {
            categoryId,
            kind = "IC",
            value = "MCU",
            footprint = "QFN32",
            type = "SMD",
            description = "Controller"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createDocument = await ParseJsonAsync(createResponse);
        var componentTypeId = createDocument.RootElement.GetProperty("id").GetGuid();

        var updateResponse = await client.PutAsJsonAsync($"/api/component-types/{componentTypeId}", new
        {
            categoryId,
            kind = "IC",
            value = "MCU-UPDATED",
            footprint = "QFN32",
            type = "SMD",
            description = "Controller updated",
            isActive = true
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await client.DeleteAsync($"/api/component-types/{componentTypeId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getDeletedResponse = await client.GetAsync($"/api/component-types/{componentTypeId}");
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ComponentTypeCreate_WhenTripleAlreadyExists_ShouldReturnConflict()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        var categoryId = await GetSeedCategoryIdAsync();
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/component-types", new
        {
            categoryId,
            kind = "Resistor",
            value = "10k",
            footprint = "0603",
            type = "SMD",
            description = "Duplicate type"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ComponentLifecycle_WhenAdminMutatesComponent_ShouldCreateUpdateAndSoftDelete()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        var (componentTypeId, supplierId) = await GetSeedInventoryReferenceIdsAsync();
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/components", new
        {
            componentTypeId,
            partNumber = "NEW-PART-001",
            batchCode = "B1",
            supplierId,
            supplierPartNumber = "SUP-NEW-001",
            unitCost = 1.25,
            minimumStockLevel = 5,
            maximumStockLevel = 50,
            reorderPoint = 10
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createDocument = await ParseJsonAsync(createResponse);
        var componentId = createDocument.RootElement.GetProperty("id").GetGuid();

        var updateResponse = await client.PutAsJsonAsync($"/api/components/{componentId}", new
        {
            componentTypeId,
            partNumber = "NEW-PART-001-UPDATED",
            batchCode = "B2",
            supplierId,
            supplierPartNumber = "SUP-NEW-002",
            unitCost = 1.50,
            minimumStockLevel = 8,
            maximumStockLevel = 60,
            reorderPoint = 12,
            isActive = true
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var updateDocument = await ParseJsonAsync(updateResponse))
        {
            updateDocument.RootElement.GetProperty("partNumber").GetString().Should().Be("NEW-PART-001-UPDATED");
        }

        var deleteResponse = await client.DeleteAsync($"/api/components/{componentId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getDeletedResponse = await client.GetAsync($"/api/components/{componentId}");
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ComponentCreate_WhenTypeReferenceDoesNotExist_ShouldReturnNotFound()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/components", new
        {
            componentTypeId = Guid.NewGuid(),
            partNumber = "MISSING-TYPE",
            unitCost = 0.5
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CategorySearch_WhenSearchMatchesName_ShouldReturnMatchingCategories()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        await client.PostAsJsonAsync("/api/component-categories", new { name = "Resistors", parentId = (Guid?)null, description = "" });
        await client.PostAsJsonAsync("/api/component-categories", new { name = "Resistor Networks", parentId = (Guid?)null, description = "" });
        await client.PostAsJsonAsync("/api/component-categories", new { name = "Capacitors", parentId = (Guid?)null, description = "" });

        var response = await client.GetAsync("/api/component-categories?search=Resist&page=1&pageSize=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = await ParseJsonAsync(response);
        var items = doc.RootElement.GetProperty("items");
        items.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);

        foreach (var item in items.EnumerateArray())
        {
            item.GetProperty("name").GetString().Should().ContainEquivalentOf("resist");
        }
    }

    [Fact]
    public async Task CategorySearch_WhenSearchIsCaseInsensitive_ShouldReturnMatches()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        await client.PostAsJsonAsync("/api/component-categories", new { name = "Inductors", parentId = (Guid?)null, description = "" });

        var lowerResponse = await client.GetAsync("/api/component-categories?search=inductor&page=1&pageSize=50");
        lowerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var lowerDoc = await ParseJsonAsync(lowerResponse);
        lowerDoc.RootElement.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        var upperResponse = await client.GetAsync("/api/component-categories?search=INDUCTOR&page=1&pageSize=50");
        upperResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var upperDoc = await ParseJsonAsync(upperResponse);
        upperDoc.RootElement.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task CategorySearch_WhenSearchDoesNotMatch_ShouldReturnEmptyItems()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/component-categories?search=xyznonexistent999&page=1&pageSize=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = await ParseJsonAsync(response);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
        doc.RootElement.GetProperty("totalItems").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task CategorySearch_WhenSearchIsOmitted_ShouldReturnAllCategories()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        await client.PostAsJsonAsync("/api/component-categories", new { name = "Cat-A", parentId = (Guid?)null, description = "" });
        await client.PostAsJsonAsync("/api/component-categories", new { name = "Cat-B", parentId = (Guid?)null, description = "" });

        var withSearch = await client.GetAsync("/api/component-categories?page=1&pageSize=100");
        withSearch.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = await ParseJsonAsync(withSearch);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task CategorySearch_WhenSearchIsSubstring_ShouldMatchMiddleOfName()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        await client.PostAsJsonAsync("/api/component-categories", new { name = "High-Speed Transistors", parentId = (Guid?)null, description = "" });
        await client.PostAsJsonAsync("/api/component-categories", new { name = "Diodes", parentId = (Guid?)null, description = "" });

        var response = await client.GetAsync("/api/component-categories?search=speed&page=1&pageSize=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = await ParseJsonAsync(response);
        var items = doc.RootElement.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("name").GetString().Should().Be("High-Speed Transistors");
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    private async Task<Guid> GetSeedCategoryIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.ComponentCategories.Select(x => x.Id).FirstAsync();
    }

    private async Task<(Guid ComponentTypeId, Guid SupplierId)> GetSeedInventoryReferenceIdsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var componentTypeId = await context.ComponentTypes.Select(x => x.Id).FirstAsync();
        var supplierId = await context.Suppliers.Select(x => x.Id).FirstAsync();
        return (componentTypeId, supplierId);
    }

    private static async Task<JsonDocument> ParseJsonAsync(HttpResponseMessage response)
    {
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    }
}
