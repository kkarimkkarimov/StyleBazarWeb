using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stylebazar.BLL.Mappings;
using Stylebazar.BLL.Rules;
using Stylebazar.BLL.Services.Account;
using Stylebazar.BLL.Services.Authentication;
using Stylebazar.BLL.Services.Categories;
using Stylebazar.BLL.Services.Contact;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.BLL.Services.LoginEvents;
using Stylebazar.BLL.Services.FileStorage;
using Stylebazar.BLL.Services.HeroBanners;
using Stylebazar.BLL.Services.Newsletter;
using Stylebazar.BLL.Services.Products;
using Stylebazar.BLL.Services.Reviews;
using Stylebazar.BLL.Services.Stores;
using Stylebazar.BLL.Services.Stories;
using Stylebazar.BLL.Services.Wishlist;
using Stylebazar.BLL.Settings;
using Stylebazar.BLL.Validators;
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

    public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
        services.AddValidatorsFromAssemblyContaining<RegisterValidator>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<FileStorageSettings>(configuration.GetSection(FileStorageSettings.SectionName));

        // File storage (local disk impl; swap to CloudinaryFileStorageService later)
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        // Auth
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthManager>();

        // Current user (needs IHttpContextAccessor — register in API/MVC startup)
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Domain services
        services.AddScoped<CategoryBusinessRules>();
        services.AddScoped<ICategoryService, CategoryManager>();

        services.AddScoped<StoreBusinessRules>();
        services.AddScoped<IStoreService, StoreManager>();

        services.AddScoped<ProductBusinessRules>();
        services.AddScoped<IProductService, ProductManager>();

        services.AddScoped<IWishlistService, WishlistManager>();

        services.AddScoped<ReviewBusinessRules>();
        services.AddScoped<IReviewService, ReviewManager>();

        services.AddScoped<StoryBusinessRules>();
        services.AddScoped<IStoryService, StoryManager>();

        services.AddScoped<IHeroBannerService, HeroBannerManager>();
        services.AddScoped<IContactMessageService, ContactMessageManager>();
        services.AddScoped<INewsletterService, NewsletterManager>();

        services.AddScoped<ILoginEventService, LoginEventManager>();
        services.AddScoped<IAccountService, AccountManager>();

        return services;
    }
}
