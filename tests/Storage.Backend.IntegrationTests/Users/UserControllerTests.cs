using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Storage.Backend.IntegrationTests.Infrastructure;
using Storage.Data;

namespace Storage.Backend.IntegrationTests.Users;

[Collection(IntegrationCollection.Name)]
public sealed class UserControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public UserControllerTests(PostgreSqlContainerFixture postgresFixture)
    {
        _factory = new CustomWebApplicationFactory(postgresFixture);
    }

    [Fact]
    public async Task UserLifecycle_WhenAdminMutatesUser_ShouldCreateListUpdateAndSoftDelete()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/users", new
        {
            username = "worker1",
            email = "worker1@test.local",
            password = "WorkerPass123!",
            role = "User",
            firstName = "Worker",
            lastName = "One"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createDocument = await ParseJsonAsync(createResponse);
        var userId = createDocument.RootElement.GetProperty("id").GetGuid();

        var listResponse = await client.GetAsync("/api/users?page=1&pageSize=50");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var listDocument = await ParseJsonAsync(listResponse))
        {
            listDocument.RootElement.GetProperty("items").EnumerateArray().Any(x => x.GetProperty("id").GetGuid() == userId).Should().BeTrue();
        }

        var updateResponse = await client.PutAsJsonAsync($"/api/users/{userId}", new
        {
            email = "worker1.updated@test.local",
            role = "ReadOnly",
            firstName = "Worker",
            lastName = "Updated",
            isActive = true
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var updateDocument = await ParseJsonAsync(updateResponse))
        {
            updateDocument.RootElement.GetProperty("email").GetString().Should().Be("worker1.updated@test.local");
            updateDocument.RootElement.GetProperty("role").GetString().Should().Be("ReadOnly");
        }

        var deleteResponse = await client.DeleteAsync($"/api/users/{userId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getDeletedResponse = await client.GetAsync($"/api/users/{userId}");
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateUser_WhenRequesterIsNotAdmin_ShouldReturnForbidden()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsUserAsync(client);

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            username = "blocked",
            email = "blocked@test.local",
            password = "BlockedPass123!",
            role = "User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUserById_WhenRequesterOwnsProfile_ShouldReturnUser()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsUserAsync(client);
        var userId = await GetUserIdByEmailAsync(TestDataSeeder.UserEmail);

        var response = await client.GetAsync($"/api/users/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = await ParseJsonAsync(response);
        document.RootElement.GetProperty("email").GetString().Should().Be(TestDataSeeder.UserEmail);
    }

    [Fact]
    public async Task GetUserById_WhenRequesterIsNotAdminAndTargetsAnotherUser_ShouldReturnForbidden()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsUserAsync(client);
        var adminId = await GetUserIdByEmailAsync(TestDataSeeder.AdminEmail);

        var response = await client.GetAsync($"/api/users/{adminId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    private async Task<Guid> GetUserIdByEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Users.IgnoreQueryFilters().Where(x => x.Email == email).Select(x => x.Id).FirstAsync();
    }

    private static async Task<JsonDocument> ParseJsonAsync(HttpResponseMessage response)
    {
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    }
}
