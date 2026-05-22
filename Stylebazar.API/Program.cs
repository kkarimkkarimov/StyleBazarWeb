using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Stylebazar.API.Middlewares;
using Stylebazar.BLL.DependencyResolver;
using Stylebazar.BLL.Settings;
using Stylebazar.DAL.DependencyResolver;
using Stylebazar.DAL.Seeders;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration)
      .ReadFrom.Services(services)
      .Enrich.FromLogContext());

// Dev safety net (see MVC/Program.cs for rationale). Injects sane
// fallback values for missing required secrets ONLY in Development.
ApplyDevFallbackSecrets(builder);

// Fail fast if required secrets are STILL missing after the fallback.
// Production hard-fails on missing env vars; Development should now
// never see this because the fallback covers all required keys.
ValidateRequiredSecrets(builder.Configuration, builder.Environment);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddIdentityServices();
builder.Services.AddBusinessServices(builder.Configuration);

JwtSettings jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>() ?? throw new InvalidOperationException("Jwt settings missing.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// Resolve file-storage RootPath against the web project's wwwroot.
string uploadsRoot = Path.Combine(
    builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot"),
    "uploads");
Directory.CreateDirectory(uploadsRoot);

builder.Services.PostConfigure<Stylebazar.BLL.Settings.FileStorageSettings>(opts =>
{
    opts.RootPath = uploadsRoot;
    opts.BaseUrl = "/uploads";
});

var app = builder.Build();

// Auto-apply pending migrations on startup (idempotent). Without this,
// a fresh clone running `dotnet run` would crash inside RoleSeeder when
// AspNetRoles doesn't exist yet.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Stylebazar.DAL.Contexts.ApplicationDbContext>();
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Database migration failed at startup. App cannot boot.");
        throw;
    }
}

await RoleSeeder.SeedAsync(app.Services);
await AdminSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "StyleBazar API";
        options.Theme = ScalarTheme.Purple;
        options.AddPreferredSecuritySchemes("Bearer");
    });
}

app.UseHttpsRedirection();

// Serve uploaded files (wwwroot/uploads → /uploads/...)
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


// ============================================================
// Helpers
// ============================================================

static void ApplyDevFallbackSecrets(WebApplicationBuilder builder)
{
    if (!builder.Environment.IsDevelopment()) return;

    var defaults = new Dictionary<string, string?>();
    if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]))
        defaults["Jwt:Key"] = "dev-only-stylebazar-fallback-key-256-bits-for-local-only";
    if (string.IsNullOrWhiteSpace(builder.Configuration["Admin:Password"]))
        defaults["Admin:Password"] = "Admin123!";

    if (defaults.Count == 0) return;

    builder.Configuration.AddInMemoryCollection(defaults);
    // Silent — see Stylebazar.MVC/Program.cs for the rationale.
}

static void ValidateRequiredSecrets(IConfiguration cfg, IHostEnvironment env)
{
    string?[] required =
    {
        cfg["Jwt:Key"],
        cfg["Admin:Password"],
        cfg.GetConnectionString("DefaultConnection")
    };
    string[] names = { "Jwt:Key", "Admin:Password", "ConnectionStrings:DefaultConnection" };

    var missing = new List<string>();
    for (int i = 0; i < required.Length; i++)
    {
        if (string.IsNullOrWhiteSpace(required[i])) missing.Add(names[i]);
    }
    if (missing.Count > 0)
    {
        throw new InvalidOperationException(
            $"Missing required configuration: {string.Join(", ", missing)}. " +
            (env.IsDevelopment()
                ? "This shouldn't happen — ApplyDevFallbackSecrets should have filled it. " +
                  "Check that the appsettings.json hasn't been edited to set an empty string."
                : "Set the matching env vars (Jwt__Key, Admin__Password, ConnectionStrings__DefaultConnection)."));
    }

    if ((cfg["Jwt:Key"] ?? "").Length < 32)
    {
        throw new InvalidOperationException(
            "Jwt:Key must be at least 32 characters (256 bits). Generate one with: openssl rand -base64 48");
    }
}
