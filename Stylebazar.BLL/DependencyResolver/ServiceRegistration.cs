using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;

namespace Stylebazar.BLL.DependencyResolver;

public static class ServiceRegistration
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services
            .AddIdentity<AppUser, AppRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.User.RequireUniqueEmail = true;

                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}
