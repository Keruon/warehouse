using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Storage.Backend.IntegrationTests.Infrastructure;
using Xunit;

namespace Storage.Backend.IntegrationTests.AuditLogs;

[Collection(IntegrationCollection.Name)]
public sealed class AuditLogControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuditLogControllerTests(PostgreSqlContainerFixture postgresFixture)
    {
        _factory = new CustomWebApplicationFactory(postgresFixture);
    }

    [Fact]
    public async Task GetAuditLogs_WhenAdminAuthenticated_ShouldReturnPaginatedLogs()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/audit-logs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetProperty("page").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetAuditLogs_WhenUserAuthenticated_ShouldReturnForbidden()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsUserAsync(client);

        var response = await client.GetAsync("/api/audit-logs");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAuditLogs_WhenReadOnlyAuthenticated_ShouldReturnForbidden()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsReadOnlyAsync(client);

        var response = await client.GetAsync("/api/audit-logs");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAuditLogs_WithoutToken_ShouldReturnUnauthorized()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/audit-logs");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAuditLogs_WithEntityTypeFilter_ShouldReturnFilteredLogs()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/audit-logs?entityType=WarehouseProject");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetAuditLogs_WithNonexistentEntityType_ShouldReturnEmpty()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/audit-logs?entityType=NonexistentType");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var items = doc.RootElement.GetProperty("items").EnumerateArray().ToList();
        items.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetAuditLogs_WithValidPageSize_ShouldRespectLimit()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/audit-logs?pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("pageSize").GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task GetAuditLogs_WithExcessivePageSize_ShouldCapAtMaximum()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/audit-logs?pageSize=10000");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("pageSize").GetInt32().Should().BeLessThanOrEqualTo(200);
    }

    [Fact]
    public async Task GetAuditTrail_WithNonexistentEntityId_ShouldReturnEmpty()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync($"/api/audit-logs/WarehouseProject/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var items = doc.RootElement.EnumerateArray().ToList();
        items.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetAuditTrail_WhenUserAuthenticated_ShouldReturnForbidden()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsUserAsync(client);

        var response = await client.GetAsync($"/api/audit-logs/WarehouseProject/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAuditTrail_WithInvalidUuidFormat_ShouldReturnError()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/audit-logs/WarehouseProject/not-a-uuid");

        // API returns 404 for invalid route parameter
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAuditTrail_WithoutToken_ShouldReturnUnauthorized()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/audit-logs/WarehouseProject/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
}
