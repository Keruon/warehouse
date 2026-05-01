using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Storage.Data;
using Storage.Helpers;
using System.Text;

namespace Storage.Backend.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainerFixture _postgresFixture;
    private const string TestIssuer = "StorageAPI-Test";
    private const string TestAudience = "StorageAPI-Test";
    private const string TestSecret = "test-secret-key-change-me-please-32chars";

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
                ["JwtSettings:Issuer"] = TestIssuer,
                ["JwtSettings:Audience"] = TestAudience,
                ["JwtSettings:SecretKey"] = TestSecret,
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

            services.PostConfigure<JwtSettings>(options =>
            {
                options.Issuer = TestIssuer;
                options.Audience = TestAudience;
                options.SecretKey = TestSecret;
                options.AccessTokenExpirationMinutes = 30;
                options.RefreshTokenExpirationDays = 7;
            });

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = TestIssuer,
                    ValidateAudience = true,
                    ValidAudience = TestAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
        });
    }
}
