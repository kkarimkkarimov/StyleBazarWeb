using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stylebazar.BLL.DependencyResolver;
using Stylebazar.DAL.DependencyResolver;
using Stylebazar.DAL.Seeders;

var builder = WebApplication.CreateBuilder(args);

// Serilog — replaces the default ILogger pipeline. Config lives in the
// "Serilog" section of appsettings*.json: console + rolling file sinks,
// minimum levels, enrichers. ReadFrom.Configuration() means redeploying
// with a different log level is a JSON edit, not a code change.
builder.Host.UseSerilog((ctx, services, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration)
      .ReadFrom.Services(services)
      .Enrich.FromLogContext());

// Dev safety net: if a developer clones the repo and hits F5 without
// first running `dotnet user-secrets set`, fill the gaps with sane
// hardcoded defaults instead of crashing. Real user-secrets or env vars
// always win because they were loaded BEFORE this runs. Production
// (env != Development) skips this fallback entirely and ValidateRequired
// Secrets below will hard-fail.
ApplyDevFallbackSecrets(builder);

// Fail fast if required secrets are STILL missing after the dev fallback.
// In Production this catches forgotten env vars; in Development it's a
// safety net for the (very rare) case where even the in-memory defaults
// couldn't be applied.
ValidateRequiredSecrets(builder.Configuration, builder.Environment);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Stylebazar.DAL.Contexts.ApplicationDbContext>(name: "db");

// Response compression — gzips dynamic HTML / JSON. .NET 10's
// MapStaticAssets already pre-compresses /css /js /lib at build time so
// we don't double-compress them; this targets controller responses.
// HTTPS-enabled by default — BREACH risk is bounded by the JSON-only
// content we send through compressed paths (no secret-mixed HTML).
builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = true;
    opts.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    opts.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    opts.MimeTypes = Microsoft.AspNetCore.ResponseCompression
        .ResponseCompressionDefaults.MimeTypes
        .Concat(new[] { "application/json", "text/html; charset=utf-8" });
});
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(o =>
    o.Level = System.IO.Compression.CompressionLevel.Fastest);
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(o =>
    o.Level = System.IO.Compression.CompressionLevel.Fastest);

builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddIdentityServices();
builder.Services.AddBusinessServices(builder.Configuration);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);

    // Cookie hardening — Identity already sets HttpOnly + SameSite=Lax by
    // default; we just lock down the production-secure flag.
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.None
        : CookieSecurePolicy.Always;
});

// ---- Rate limiting (Phase 9) ----
// Per-IP fixed-window buckets. Numbers tuned for our scale: tight on the
// auth endpoints (brute-force defense layered on top of Identity lockout),
// generous on signed-in API surface so the admin dashboard never throttles.
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;

    // Most aggressive — login + forgot password. Trip wires brute-force
    // scripts well before Identity's 5-strike lockout kicks in.
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

    // Mid — register, contact form, address create, store actions.
    opts.AddPolicy("write-default", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    opts.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        if (context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsync(
                """{"ok":false,"error":"Çox tez-tez cəhd. Bir dəqiqə sonra yenidən cəhd et."}""",
                ct);
        }
        else
        {
            context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
            await context.HttpContext.Response.WriteAsync(
                "Çox tez-tez cəhd. Bir dəqiqə sonra yenidən cəhd et.", ct);
        }
    };
});

// Resolve file-storage RootPath against the web project's wwwroot.
string mvcUploadsRoot = Path.Combine(
    builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot"),
    "uploads");
Directory.CreateDirectory(mvcUploadsRoot);

builder.Services.PostConfigure<Stylebazar.BLL.Settings.FileStorageSettings>(opts =>
{
    opts.RootPath = mvcUploadsRoot;
    opts.BaseUrl = "/uploads";
});

var app = builder.Build();

// Auto-apply pending migrations on startup. Idempotent — no-op when DB
// is already at HEAD. Critical for fresh-clone F5 flow: without this,
// RoleSeeder would crash trying to query the AspNetRoles table because
// the tables don't exist yet. Wrapped so a migration failure (e.g.
// permission denied) surfaces with a clear message instead of dying
// inside a seeder.
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
await SubscriptionSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    // HTTPS redirect only in non-dev — keeps the dev startup log clean
    // (Kestrel logs "Failed to determine the https port for redirect" when
    // no HTTPS listener is bound, which the default dev profile omits).
    app.UseHttpsRedirection();
}

// Friendly 404 / 403 / 500 — re-execute through /Home/Error so the layout
// (header, footer, brand) renders consistently around the error message.
app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");

// ---- Security headers (Phase 9) ----
// Applied to every response (HTML + static + API). Minimal CSP — fonts.gstatic
// and Google Fonts CSS are allow-listed; img/connect-src kept wide to let
// uploaded products and the upcoming Cloudinary swap render without churn.
app.Use(async (ctx, next) =>
{
    var headers = ctx.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

    // CSP — kept pragmatic for an MVP. Production tightens img-src/connect-src
    // to the actual CDN + domains being used.
    if (!headers.ContainsKey("Content-Security-Policy"))
    {
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com data:; " +
            "img-src 'self' data: blob: https:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'self'; " +
            "base-uri 'self'; " +
            "form-action 'self';";
    }
    await next();
});

// Compression runs as early as possible so all downstream middleware
// gets the compressed Response.Body wrapper.
//
// Disabled in Development because it breaks Visual Studio's Browser Link
// + .NET hot-reload script injection ("Unable to configure Browser Link
// script injection ... Content-Encoding: '[br]'. Consider disabling
// response compression."). Behind a reverse proxy in prod nginx will
// usually do compression for us anyway; this is the in-process fallback
// for environments where it doesn't.
if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
}

app.UseRouting();

// Rate limiter sits between routing and auth so endpoint metadata
// (like [EnableRateLimiting]) is already resolved.
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Liveness + readiness check — wired to DbContext so a DB outage shows
// "Unhealthy" without polling controllers.
app.MapHealthChecks("/health");

app.MapStaticAssets();

// Admin area first so /Admin/* never falls through to the catch-all route.
app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();


// ============================================================
// Helpers
// ============================================================

/// <summary>
/// Development-only safety net. If a missing required secret is detected,
/// inject a hardcoded fallback into the configuration via in-memory
/// provider. Skipped in non-Development environments so production still
/// hard-fails on missing secrets.
///
/// Silent by default — devs running locally don't need an alarm in their
/// terminal every boot. The fallback values are clearly self-identifying
/// ("dev-only-stylebazar-fallback...") so they're easy to spot if they
/// ever leak somewhere.
/// </summary>
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
    // No console output: every dev launch would print this even when
    // user-secrets are correctly configured (e.g. when VS's launch
    // context silently fails to forward them to the secret-provider
    // assembly resolution). The fallback values are designed to "just
    // work" for local dev; if someone needs the real user-secrets they'll
    // see it through their normal env (Login still works, etc.).
}

static void ValidateRequiredSecrets(IConfiguration cfg, IHostEnvironment env)
{
    // Three secrets are mandatory in every environment. In Development
    // ApplyDevFallbackSecrets has already filled any gaps. In Production
    // they must come from env vars (Jwt__Key, Admin__Password,
    // ConnectionStrings__DefaultConnection) — anything missing here means
    // an operator forgot a step and we refuse to boot.
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

    // Extra check: minimum JWT key length. Tokens signed with < 256 bits
    // are rejected by the JwtBearer handler at issue time, so fail earlier
    // here for a clearer error message.
    if ((cfg["Jwt:Key"] ?? "").Length < 32)
    {
        throw new InvalidOperationException(
            "Jwt:Key must be at least 32 characters (256 bits). Generate one with: openssl rand -base64 48");
    }
}
