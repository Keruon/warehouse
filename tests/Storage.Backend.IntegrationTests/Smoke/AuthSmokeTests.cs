using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Storage.Backend.IntegrationTests.Infrastructure;

namespace Storage.Backend.IntegrationTests.Smoke;

[Collection(IntegrationCollection.Name)]
public sealed class AuthSmokeTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthSmokeTests(PostgreSqlContainerFixture postgresFixture)
    {
        _factory = new CustomWebApplicationFactory(postgresFixture);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task Login_WhenAdminCredentialsAreValid_ShouldReturnAccessToken()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);

        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            usernameOrEmail = TestDataSeeder.AdminEmail,
            password = TestDataSeeder.AdminPassword
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var accessToken = await AuthTestHelper.LoginAsAdminAsync(client);
        accessToken.Should().NotBeNullOrWhiteSpace();
    }
}
