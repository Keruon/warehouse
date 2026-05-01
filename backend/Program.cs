using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NpgsqlTypes;
using Serilog;
using Serilog.Sinks.PostgreSQL;
using Storage.Data;
using Storage.Data.Repositories;
using Storage.Helpers;
using Storage.Middleware;
using Storage.Services;
using Storage.Services.Auth;
using Storage.Services.Projects;
using Storage.Services.Search;
using Storage.Services.Stock;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();

    if (!context.HostingEnvironment.IsDevelopment())
    {
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var columns = new Dictionary<string, ColumnWriterBase>
            {
                ["message"] = new RenderedMessageColumnWriter(),
                ["message_template"] = new MessageTemplateColumnWriter(),
                ["level"] = new LevelColumnWriter(true, NpgsqlDbType.Varchar),
                ["raise_date"] = new TimestampColumnWriter(),
                ["exception"] = new ExceptionColumnWriter(),
                ["properties"] = new LogEventSerializedColumnWriter(),
                ["props_test"] = new PropertiesColumnWriter(NpgsqlDbType.Jsonb)
            };

            configuration.WriteTo.PostgreSQL(
                connectionString,
                context.Configuration["SerilogPostgreSql:TableName"] ?? "application_logs",
                columns,
                needAutoCreateTable: true);
        }
    }
});

// Add DbContext services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Infrastructure services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IComponentRepository, ComponentRepository>();
builder.Services.AddScoped<IStockLocationRepository, StockLocationRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IProjectLocationService, ProjectLocationService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<JwtTokenHelper>();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings configuration is missing.");

builder.Services.AddHttpContextAccessor();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddMediatR(typeof(Program));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
                .AllowAnyMethod()
                .AllowAnyHeader();
    });
});

// Add JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole(UserRole.Admin.ToString()));
    options.AddPolicy("User", policy => policy.RequireRole(UserRole.User.ToString(), UserRole.Admin.ToString()));
    options.AddPolicy("ReadOnly", policy => policy.RequireRole(UserRole.ReadOnly.ToString(), UserRole.User.ToString(), UserRole.Admin.ToString()));
});

builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("database");

// Add controllers
builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    // Allow enum values like "Storage" from frontend payloads.
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Storage API", Version = "v1" });
    c.TagActionsBy(api => [api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] ?? "API"]);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token as: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.ExecuteSqlRaw(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'zone_type_enum') THEN
        CREATE TYPE zone_type_enum AS ENUM ('Storage', 'Production', 'Shipping', 'Returns', 'Maintenance');
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'component_type_enum') THEN
        CREATE TYPE component_type_enum AS ENUM ('SMD', 'ThroughHole', 'QFP', 'SOIC', 'DIP', 'Other');
    END IF;
END $$;
");
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database migration skipped during startup.");
    }
}

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions());
app.MapControllers();

app.Run();