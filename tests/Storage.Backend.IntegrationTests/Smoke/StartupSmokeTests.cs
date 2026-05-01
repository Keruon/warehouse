using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Storage.Backend.IntegrationTests.Infrastructure;

namespace Storage.Backend.IntegrationTests.Smoke;

[Collection(IntegrationCollection.Name)]
public sealed class StartupSmokeTests
{
    private readonly CustomWebApplicationFactory _factory;

    public StartupSmokeTests(PostgreSqlContainerFixture postgresFixture)
    {
        _factory = new CustomWebApplicationFactory(postgresFixture);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task HealthEndpoint_WhenAppStarts_ShouldReturnOk()
    {
        await TestDataSeeder.SeedAsync(_factory.Services);

        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
