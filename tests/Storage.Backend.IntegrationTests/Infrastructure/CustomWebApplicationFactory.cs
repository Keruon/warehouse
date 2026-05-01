using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Storage.Data;

namespace Storage.Backend.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainerFixture _postgresFixture;

    public CustomWebApplicationFactory(PostgreSqlContainerFixture postgresFixture)
    {
        _postgresFixture = postgresFixture;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var inMemory = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgresFixture.ConnectionString,
                ["JwtSettings:Issuer"] = "StorageAPI-Test",
                ["JwtSettings:Audience"] = "StorageAPI-Test",
                ["JwtSettings:SecretKey"] = "test-secret-key-change-me-please-32chars",
                ["JwtSettings:AccessTokenExpirationMinutes"] = "30",
                ["JwtSettings:RefreshTokenExpirationDays"] = "7"
            };

            config.AddInMemoryCollection(inMemory);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_postgresFixture.ConnectionString));
        });
    }
}
