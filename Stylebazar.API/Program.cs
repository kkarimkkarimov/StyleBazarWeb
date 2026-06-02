using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
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

// ──────────────────────────────────────────────────────────────
// Production hardening — parity with the MVC app
// ──────────────────────────────────────────────────────────────

// Reverse proxy (X-Forwarded-*). Behind nginx/IIS the real client IP + scheme
// arrive in forwarded headers. Without this the rate limiter keys every request
// on the proxy's loopback IP (one global bucket instead of per-IP) and HTTPS
// redirect can loop. Loopback is trusted by default; extra proxy IPs (container/
// k8s) come from config: ForwardedHeaders:KnownProxies = [ "10.0.0.5", ... ].
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
    o.KnownProxies.Add(IPAddress.Loopback);
    o.KnownProxies.Add(IPAddress.IPv6Loopback);
    foreach (var ip in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
        if (IPAddress.TryParse(ip, out var addr)) o.KnownProxies.Add(addr);
});

// CORS — browser clients (SPA / mobile webview) calling the API cross-origin.
// Production: explicit allow-list from config (Cors:Origins). Development:
// reflect any origin so local front-ends on random ports work without per-port
// config. Empty list in Production = no cross-origin access (safe default).
builder.Services.AddCors(o => o.AddPolicy("frontend", p =>
{
    var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
    if (origins.Length > 0)
        p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    else if (builder.Environment.IsDevelopment())
        p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));

// Health — liveness/readiness wired to the DbContext so a DB outage surfaces as
// "Unhealthy" at /health without polling controllers.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Stylebazar.DAL.Contexts.ApplicationDbContext>(name: "db");

// Response compression for JSON payloads (HTTPS-enabled; BREACH risk bounded —
// API responses carry no secret-mixed HTML).
builder.Services.AddResponseCompression(o => o.EnableForHttps = true);

// Rate limiting — per-IP fixed window. A generous GlobalLimiter protects every
// endpoint automatically; the tight "auth-strict" policy is opt-in for login /
// token endpoints via [EnableRateLimiting("auth-strict")].
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;

    opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    opts.AddPolicy("auth-strict", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    opts.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            """{"message":"Too many requests. Please try again in a minute."}""", ct);
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

// Reverse-proxy headers FIRST so every downstream middleware (rate limiter,
// HTTPS redirect, auth) sees the real client IP + scheme. No-op in dev.
app.UseForwardedHeaders();

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

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseResponseCompression();
}

app.UseHttpsRedirection();

// Security headers on every response (JSON + served upload files). No CSP — this
// is a JSON API, not an HTML surface; nosniff + DENY framing + tight referrer
// are the relevant ones.
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "no-referrer";
    h["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    await next();
});

// Serve uploaded files (wwwroot/uploads → /uploads/...)
app.UseStaticFiles();

app.UseRouting();

// CORS after routing, before auth (preflight must not be blocked by auth).
app.UseCors("frontend");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Liveness/readiness — DB-backed, mirrors the MVC endpoint.
app.MapHealthChecks("/health");
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
