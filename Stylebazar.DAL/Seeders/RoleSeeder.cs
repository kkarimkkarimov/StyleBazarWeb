using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Seeders;

public static class RoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        ILogger logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(RoleSeeder).FullName!);

        // Wait for LocalDB / SQL Server to be reachable.
        // Useful when LocalDB is cold-starting and EF's built-in
        // retry policy doesn't cover hard-failure connection errors.
        await WaitForDatabaseAsync(scope.ServiceProvider, logger);

        RoleManager<AppRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

        foreach (UserRole role in Enum.GetValues<UserRole>())
        {
            string roleName = role.ToString();
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            await roleManager.CreateAsync(new AppRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = roleName
            });
        }
    }

    private static async Task WaitForDatabaseAsync(IServiceProvider provider, ILogger logger)
    {
        const int maxAttempts = 10;
        TimeSpan delay = TimeSpan.FromSeconds(2);

        ApplicationDbContext db = provider.GetRequiredService<ApplicationDbContext>();

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (await db.Database.CanConnectAsync())
                    return;

                logger.LogWarning("Database not yet reachable (attempt {Attempt}/{Max}).", attempt, maxAttempts);
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Database connection attempt {Attempt}/{Max} failed; retrying in {Delay}s.",
                    attempt, maxAttempts, delay.TotalSeconds);
            }

            await Task.Delay(delay);
        }

        throw new InvalidOperationException(
            $"Database was not reachable after {maxAttempts} attempts. Make sure LocalDB / SQL Server is running.");
    }
}
