using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Stylebazar.BLL.DependencyResolver;
using Stylebazar.DAL.DependencyResolver;
using Stylebazar.DAL.Seeders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Stylebazar.DAL.Contexts.ApplicationDbContext>(name: "db");

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
