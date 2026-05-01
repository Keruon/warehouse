using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Storage.Backend.IntegrationTests.Infrastructure;

public static class AuthTestHelper
{
    public sealed record LoginResult(string AccessToken, string RefreshToken, JsonElement Payload);

    public static async Task<string> LoginAsAdminAsync(HttpClient client, CancellationToken cancellationToken = default)
        => (await LoginAsync(client, TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword, cancellationToken)).AccessToken;

    public static async Task<string> LoginAsUserAsync(HttpClient client, CancellationToken cancellationToken = default)
        => (await LoginAsync(client, TestDataSeeder.UserEmail, TestDataSeeder.UserPassword, cancellationToken)).AccessToken;

    public static async Task<string> LoginAsReadOnlyAsync(HttpClient client, CancellationToken cancellationToken = default)
        => (await LoginAsync(client, TestDataSeeder.ReadOnlyEmail, TestDataSeeder.ReadOnlyPassword, cancellationToken)).AccessToken;

    public static async Task<LoginResult> LoginAsync(
        HttpClient client,
        string usernameOrEmail,
        string password,
        CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                usernameOrEmail,
                password
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var payload = document.RootElement.Clone();
        var tokens = payload.GetProperty("data").GetProperty("tokens");

        return new LoginResult(
            tokens.GetProperty("accessToken").GetString()
                ?? throw new InvalidOperationException("Access token missing from login response."),
            tokens.GetProperty("refreshToken").GetString()
                ?? throw new InvalidOperationException("Refresh token missing from login response."),
            payload);
    }

    public static void ApplyBearerToken(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public static async Task AuthenticateAsAdminAsync(HttpClient client, CancellationToken cancellationToken = default)
    {
        var token = await LoginAsAdminAsync(client, cancellationToken);
        ApplyBearerToken(client, token);
    }

    public static async Task AuthenticateAsUserAsync(HttpClient client, CancellationToken cancellationToken = default)
    {
        var token = await LoginAsUserAsync(client, cancellationToken);
        ApplyBearerToken(client, token);
    }

    public static async Task AuthenticateAsReadOnlyAsync(HttpClient client, CancellationToken cancellationToken = default)
    {
        var token = await LoginAsReadOnlyAsync(client, cancellationToken);
        ApplyBearerToken(client, token);
    }
}
