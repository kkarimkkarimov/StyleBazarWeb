using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Repositories.Abstract;
using Stylebazar.DAL.Repositories.Concrete;

namespace Stylebazar.DAL.DependencyResolver;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql
                    .EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(3),
                        errorNumbersToAdd: null)
                    // Multi-collection Include queries (Product → Images + Sizes +
                    // Colors + Reviews) cause cartesian explosion under the default
                    // SingleQuery mode AND emit a MultipleCollectionIncludeWarning
                    // at runtime. SplitQuery runs N small queries instead — fewer
                    // bytes over the wire, no warning.
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<IProductSizeRepository, ProductSizeRepository>();
        services.AddScoped<IProductColorRepository, ProductColorRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReviewImageRepository, ReviewImageRepository>();
        services.AddScoped<IStoryRepository, StoryRepository>();
        services.AddScoped<IHeroBannerRepository, HeroBannerRepository>();
        services.AddScoped<IContactMessageRepository, ContactMessageRepository>();
        services.AddScoped<INewsletterSubscriberRepository, NewsletterSubscriberRepository>();
        services.AddScoped<ILoginEventRepository, LoginEventRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IStoreFollowerRepository, StoreFollowerRepository>();
        services.AddScoped<IContactClickRepository, ContactClickRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

        return services;
    }
}
