using System.Text;
using Dourak.Api.Middleware;
using Dourak.Application;
using Dourak.Infrastructure;
using Dourak.Infrastructure.Identity;
using Dourak.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

// Bootstrap logger — captures anything that happens before the host's own logging
// pipeline (built from appsettings below) is ready, e.g. config-loading failures.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Serilog replaces the default provider entirely. Every sink (console/file/Seq),
// its args, and minimum levels are read entirely from the "Serilog" section in
// appsettings.json / appsettings.Development.json via Serilog.Settings.Configuration
// (ReadFrom.Configuration) — no sink is constructed in code. Seq is simply absent
// from the base appsettings.json's WriteTo array; appsettings.Development.json adds
// it back for local dev, and docker-compose.yml adds it back in production via
// Serilog__WriteTo__2__Name / __Args__serverUrl env vars (same pattern already used
// for Cors__AllowedOrigins__0) — so it's still opt-in per environment, just entirely
// through configuration instead of an `if` in Program.cs.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Dourak.Api"));

// ----- Services -----

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Dourak API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT token."
    });
    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer"), new List<string>() }
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// MCP server (Model Context Protocol) — lets an AI assistant (ChatGPT, Claude, ...) read a
// signed-in user's circles/payments and act on their behalf (report a payment, set a reminder),
// mapped at /mcp below and gated behind the exact same JWT bearer auth as every controller — see
// Mcp/DourakMcpTools.cs for why no tool ever needs (or accepts) a user id parameter.
builder.Services.AddMcpServer().WithHttpTransport().WithToolsFromAssembly();

var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
var jwtSecret = jwtSection["Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
    options.Events = new JwtBearerEvents
    {
        // MCP clients that speak the OAuth-based authorization spec (Claude Desktop) discover
        // this server's OAuth endpoints from this header on a bare 401 — see
        // /.well-known/oauth-protected-resource in OAuthController.
        OnChallenge = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api/mcp"))
            {
                var baseUrl = builder.Configuration.GetSection("App")["FrontendBaseUrl"]?.TrimEnd('/') ?? "";
                context.Response.Headers.Append("WWW-Authenticate",
                    $"Bearer resource_metadata=\"{baseUrl}/.well-known/oauth-protected-resource\"");
            }
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();

const string CorsPolicy = "DourakClient";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" })
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// ----- Pipeline -----

app.UseSerilogRequestLogging(); // one structured line per HTTP request (method, path, status, elapsed ms)
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Under /api/mcp (not just /mcp) so it's reachable through the same Caddy `/api/*` reverse-proxy
// block the rest of the API already uses in production — no separate infra/DNS/cert needed.
app.MapMcp("/api/mcp").RequireAuthorization();

// Apply migrations automatically on startup — acceptable simplicity for an MVP (see README).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DourakDbContext>();
    db.Database.Migrate();

    // prompt03 §4 — TEMPORARY beta-testing mechanism: seed four fixed test accounts
    // (idempotent). Remove before real production launch (see docs/future-work.md).
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    await BetaUserSeeder.SeedAsync(userManager, app.Logger);

    // Admin site (admin.dourak.money): ensures the "Admin" role exists, and — when
    // Admin:Email/Password are configured (see .env.example) — that account has it.
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var adminOptions = scope.ServiceProvider.GetRequiredService<IOptions<AdminOptions>>();
    await AdminSeeder.SeedAsync(userManager, roleManager, adminOptions, app.Logger);
}

try
{
    Log.Information("Starting Dourak.Api");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Dourak.Api terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { } // exposed for WebApplicationFactory in integration tests
