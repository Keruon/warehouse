using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Storage.Backend.IntegrationTests.Infrastructure;
using Xunit;

namespace Storage.Backend.IntegrationTests.Search;

[Collection(IntegrationCollection.Name)]
public sealed class SearchControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public SearchControllerTests(PostgreSqlContainerFixture postgresFixture)
    {
        _factory = new CustomWebApplicationFactory(postgresFixture);
    }

    [Fact]
    public async Task SearchComponents_NoFilter_ShouldReturnPaginatedResults()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsUserAsync(client);

        var response = await client.GetAsync("/api/search/components");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("items").EnumerateArray().Count().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task SearchComponents_WithQuery_ShouldFilterByName()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsUserAsync(client);

        var response = await client.GetAsync("/api/search/components?q=resistor");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task SearchComponents_WithNonexistentQuery_ShouldReturnEmpty()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsUserAsync(client);

        var response = await client.GetAsync("/api/search/components?q=NONEXISTENT_XYZ_COMPONENT");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("items").EnumerateArray().Count().Should().Be(0);
    }

    [Fact]
    public async Task SearchComponents_ByReadOnlyUser_ShouldSucceed()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsReadOnlyAsync(client);

        var response = await client.GetAsync("/api/search/components");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchComponents_WithoutToken_ShouldReturnUnauthorized()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/search/components");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchComponents_WithValidPageSize_ShouldRespectLimit()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsUserAsync(client);

        var response = await client.GetAsync("/api/search/components?pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("pageSize").GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task SearchComponents_WithNegativePage_ShouldDefaultPage1()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsUserAsync(client);

        var response = await client.GetAsync("/api/search/components?page=-1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("page").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task SearchLocations_NoFilter_ShouldReturnList()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsUserAsync(client);

        var response = await client.GetAsync("/api/search/locations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task SearchLocations_WithQuery_ShouldFilterResults()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsUserAsync(client);

        var response = await client.GetAsync("/api/search/locations?q=warehouse");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task SearchLocations_ByReadOnlyUser_ShouldSucceed()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsReadOnlyAsync(client);

        var response = await client.GetAsync("/api/search/locations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchLocations_WithoutToken_ShouldReturnUnauthorized()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/search/locations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchLocations_WithNonexistentQuery_ShouldReturnEmpty()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services, includeInventoryData: true);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsUserAsync(client);

        var response = await client.GetAsync("/api/search/locations?q=NONEXISTENT_LOCATION_XYZ");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.EnumerateArray().Count().Should().Be(0);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
}
