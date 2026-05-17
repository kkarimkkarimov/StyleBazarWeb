using System.Linq.Expressions;
using Core.Persistence.Repositories;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Stylebazar.DAL.Configurations;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Contexts;

public class ApplicationDbContext : IdentityDbContext<AppUser, AppRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductSize> ProductSizes => Set<ProductSize>();
    public DbSet<ProductColor> ProductColors => Set<ProductColor>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewImage> ReviewImages => Set<ReviewImage>();
    public DbSet<Story> Stories => Set<Story>();
    public DbSet<HeroBanner> HeroBanners => Set<HeroBanner>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();
    public DbSet<LoginEvent> LoginEvents => Set<LoginEvent>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<StoreFollower> StoreFollowers => Set<StoreFollower>();

    public override int SaveChanges()
    {
        ApplyAuditFields();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StoreConfiguration).Assembly);

        ApplySoftDeleteQueryFilter(modelBuilder);
    }

    private void ApplyAuditFields()
    {
        DateTime now = DateTime.UtcNow;
        foreach (EntityEntry entry in ChangeTracker.Entries())
        {
            if (!InheritsFromEntity(entry.Entity.GetType()))
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(Entity<int>.CreatedDate)).CurrentValue = now;
                    break;
                case EntityState.Modified:
                    entry.Property(nameof(Entity<int>.UpdatedDate)).CurrentValue = now;
                    break;
            }
        }
    }

    private static void ApplySoftDeleteQueryFilter(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!InheritsFromEntity(entityType.ClrType))
                continue;

            ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "e");
            MemberExpression deletedDateProperty = Expression.Property(parameter, nameof(Entity<int>.DeletedDate));
            ConstantExpression nullValue = Expression.Constant(null, typeof(DateTime?));
            BinaryExpression isNotDeleted = Expression.Equal(deletedDateProperty, nullValue);
            LambdaExpression filter = Expression.Lambda(isNotDeleted, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }

    private static bool InheritsFromEntity(Type type)
    {
        Type? current = type.BaseType;
        while (current is not null && current != typeof(object))
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Entity<>))
                return true;
            current = current.BaseType;
        }
        return false;
    }
}
