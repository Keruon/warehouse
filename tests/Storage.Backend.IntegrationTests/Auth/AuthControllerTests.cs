using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Storage.Backend.IntegrationTests.Infrastructure;

namespace Storage.Backend.IntegrationTests.Auth;

[Collection(IntegrationCollection.Name)]
public sealed class AuthControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(PostgreSqlContainerFixture postgresFixture)
    {
        _factory = new CustomWebApplicationFactory(postgresFixture);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ShouldReturnTokensAndUser()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            usernameOrEmail = TestDataSeeder.AdminEmail,
            password = TestDataSeeder.AdminPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        document.RootElement.GetProperty("data").GetProperty("user").GetProperty("email").GetString().Should().Be(TestDataSeeder.AdminEmail);
        document.RootElement.GetProperty("data").GetProperty("tokens").GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.GetProperty("data").GetProperty("tokens").GetProperty("refreshToken").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WhenPasswordIsInvalid_ShouldReturnUnauthorized()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            usernameOrEmail = TestDataSeeder.AdminEmail,
            password = "wrong-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WhenAuthenticated_ShouldReturnCurrentUser()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        document.RootElement.GetProperty("data").GetProperty("email").GetString().Should().Be(TestDataSeeder.AdminEmail);
        document.RootElement.GetProperty("data").GetProperty("role").GetString().Should().Be("Admin");
    }

    [Fact]
    public async Task Refresh_WhenRefreshTokenIsValid_ShouldReturnNewTokens()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        var login = await AuthTestHelper.LoginAsync(client, TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = login.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        document.RootElement.GetProperty("data").GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.GetProperty("data").GetProperty("refreshToken").GetString().Should().NotBe(login.RefreshToken);
    }

    [Fact]
    public async Task Logout_WhenRefreshTokenIsValid_ShouldRevokeToken()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        var login = await AuthTestHelper.LoginAsync(client, TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);

        var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken = login.RefreshToken
        });

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = login.RefreshToken
        });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }
}
