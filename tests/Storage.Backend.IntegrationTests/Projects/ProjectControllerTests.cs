using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Storage.Backend.IntegrationTests.Infrastructure;
using Storage.Data;

namespace Storage.Backend.IntegrationTests.Projects;

[Collection(IntegrationCollection.Name)]
public sealed class ProjectControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public ProjectControllerTests(PostgreSqlContainerFixture postgresFixture)
    {
        _factory = new CustomWebApplicationFactory(postgresFixture);
    }

    [Fact]
    public async Task CreateProject_WhenUserIsAuthenticated_ShouldReturnCreatedWithNormalizedCode()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsUserAsync(client);

        var response = await client.PostAsJsonAsync("/api/projects", new { name = "Project Alpha", code = " alpha-1 " });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        document.RootElement.GetProperty("name").GetString().Should().Be("Project Alpha");
        document.RootElement.GetProperty("code").GetString().Should().Be("ALPHA-1");
    }

    [Fact]
    public async Task CreateProject_WhenCodeAlreadyExists_ShouldReturnBadRequest()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var first = await client.PostAsJsonAsync("/api/projects", new { name = "First Project", code = "DUP-1" });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/projects", new { name = "Second Project", code = "dup-1" });
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var document = await JsonDocument.ParseAsync(await second.Content.ReadAsStreamAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("duplicate_project_code");
    }

    [Fact]
    public async Task SetAndClearActiveProject_WhenProjectExists_ShouldRoundTrip()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);
        var projectId = await CreateProjectAsync(client, "Active Project", "ACTIVE-1");

        var setActive = await client.PutAsync($"/api/projects/active/{projectId}", content: null);
        setActive.StatusCode.Should().Be(HttpStatusCode.OK);

        var getActive = await client.GetAsync("/api/projects/active");
        getActive.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var document = await JsonDocument.ParseAsync(await getActive.Content.ReadAsStreamAsync()))
        {
            document.RootElement.GetProperty("activeProject").GetProperty("id").GetGuid().Should().Be(projectId);
        }

        var clearActive = await client.DeleteAsync("/api/projects/active");
        clearActive.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterClear = await client.GetAsync("/api/projects/active");
        using var clearedDocument = await JsonDocument.ParseAsync(await getAfterClear.Content.ReadAsStreamAsync());
        clearedDocument.RootElement.GetProperty("activeProject").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task DeactivateAndActivate_WhenProjectWasActive_ShouldClearAndRestoreAvailability()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);
        var projectId = await CreateProjectAsync(client, "Lifecycle Project", "LIFE-1");
        (await client.PutAsync($"/api/projects/active/{projectId}", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var deactivate = await client.PutAsync($"/api/projects/{projectId}/deactivate", null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.OK);

        var setInactive = await client.PutAsync($"/api/projects/active/{projectId}", null);
        setInactive.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var getActive = await client.GetAsync("/api/projects/active");
        using (var document = await JsonDocument.ParseAsync(await getActive.Content.ReadAsStreamAsync()))
        {
            document.RootElement.GetProperty("activeProject").ValueKind.Should().Be(JsonValueKind.Null);
        }

        var activate = await client.PutAsync($"/api/projects/{projectId}/activate", null);
        activate.StatusCode.Should().Be(HttpStatusCode.OK);

        var setActiveAgain = await client.PutAsync($"/api/projects/active/{projectId}", null);
        setActiveAgain.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteProject_WhenProjectExists_ShouldWriteAuditLog()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(client);
        var projectId = await CreateProjectAsync(client, "Delete Project", "DEL-1");

        var delete = await client.DeleteAsync($"/api/projects/{projectId}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var log = await context.AuditLogs.OrderByDescending(x => x.Timestamp).FirstOrDefaultAsync(x => x.EntityId == projectId && x.Action == "DELETE_PROJECT");
        log.Should().NotBeNull();
        log!.EntityType.Should().Be("Project");
    }

    [Fact]
    public async Task CloseProject_WhenUserIsNotAdmin_ShouldReturnForbidden()
    {
        await TestDataSeeder.ResetAndSeedAsync(_factory.Services);
        using var client = CreateClient();
        await AuthTestHelper.AuthenticateAsUserAsync(client);
        var adminClient = CreateClient();
        await AuthTestHelper.AuthenticateAsAdminAsync(adminClient);
        var projectId = await CreateProjectAsync(adminClient, "Close Project", "CLOSE-1");

        var response = await client.PostAsJsonAsync($"/api/projects/{projectId}/close", new { confirm = true });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient client, string name, string code)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new { name, code });
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement.GetProperty("id").GetGuid();
    }
}
