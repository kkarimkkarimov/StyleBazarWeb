using Stylebazar.BLL.DependencyResolver;
using Stylebazar.DAL.DependencyResolver;
using Stylebazar.DAL.Seeders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddIdentityServices();
builder.Services.AddBusinessServices(builder.Configuration);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
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

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
