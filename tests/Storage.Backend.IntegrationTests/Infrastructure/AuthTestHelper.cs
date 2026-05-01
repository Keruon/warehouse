using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Storage.Backend.IntegrationTests.Infrastructure;

public static class AuthTestHelper
{
    public static async Task<string> LoginAsAdminAsync(HttpClient client, CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                usernameOrEmail = TestDataSeeder.AdminEmail,
                password = TestDataSeeder.AdminPassword
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return document.RootElement
            .GetProperty("data")
            .GetProperty("tokens")
            .GetProperty("accessToken")
            .GetString()
            ?? throw new InvalidOperationException("Access token missing from login response.");
    }

    public static async Task AuthenticateAsAdminAsync(HttpClient client, CancellationToken cancellationToken = default)
    {
        var token = await LoginAsAdminAsync(client, cancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
