using System.Linq.Expressions;
using Core.Persistence.Repositories;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Stylebazar.DAL.Configurations;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Contexts;

// VERİLƏNLƏR BAZASI KONTEKSTİ — bütün baza ilə işin MƏRKƏZİ nöqtəsi.
// IdentityDbContext-dən miras alır → istifadəçi/rol cədvəlləri (Identity) avtomatik gəlir.
// Hər DbSet bir cədvələ uyğundur; EF Core bu sinif vasitəsilə C# obyektlərini SQL-ə çevirir.
public class ApplicationDbContext : IdentityDbContext<AppUser, AppRole, string>
{
    // Ayarları (connection string və s.) DI (asılılıq inyeksiyası) ilə qəbul edir.
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // --- DbSet-lər: hər biri bazada BİR cədvələ uyğundur. Sorğular buradan başlayır. ---
    public DbSet<Store> Stores => Set<Store>();                                   // Mağazalar
    public DbSet<Category> Categories => Set<Category>();                          // Kateqoriyalar
    public DbSet<Product> Products => Set<Product>();                             // Məhsullar
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();             // Məhsul şəkilləri
    public DbSet<ProductSize> ProductSizes => Set<ProductSize>();                // Məhsul ölçüləri
    public DbSet<ProductColor> ProductColors => Set<ProductColor>();             // Məhsul rəngləri
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();                         // Seçilmişlər
    public DbSet<Review> Reviews => Set<Review>();                               // Rəylər
    public DbSet<ReviewImage> ReviewImages => Set<ReviewImage>();                // Rəy şəkilləri
    public DbSet<Story> Stories => Set<Story>();                                 // Hekayələr
    public DbSet<HeroBanner> HeroBanners => Set<HeroBanner>();                   // Ana səhifə bannerləri
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();       // Əlaqə mesajları
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>(); // Bülleten abunəçiləri
    public DbSet<LoginEvent> LoginEvents => Set<LoginEvent>();                   // Giriş hadisələri
    public DbSet<Address> Addresses => Set<Address>();                           // Ünvanlar
    public DbSet<StoreFollower> StoreFollowers => Set<StoreFollower>();          // Mağaza izləyiciləri
    public DbSet<ContactClick> ContactClicks => Set<ContactClick>();            // Əlaqə klikləri (analitika)
    public DbSet<Subscription> Subscriptions => Set<Subscription>();             // Abunəliklər
    public DbSet<Conversation> Conversations => Set<Conversation>();            // Söhbətlər
    public DbSet<Message> Messages => Set<Message>();                            // Mesajlar

    // --- SaveChanges override-ları ---
    // Bazaya yazmadan ƏVVƏL audit sahələrini (CreatedDate/UpdatedDate) avtomatik doldururuq.
    // 4 variantın hamısı override olunub ki, sinxron/asinxron bütün çağırışlar eyni davransın.
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

    // Model (cədvəllərin quruluşu) yaradılarkən BİR dəfə çağırılır.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);  // Identity-nin öz cədvəl konfiqurasiyaları

        // Configurations qovluğundakı BÜTÜN IEntityTypeConfiguration siniflərini avtomatik tap və tətbiq et.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StoreConfiguration).Assembly);

        // Hər entity-yə "silinmişləri gizlət" filtrini qoş (soft-delete).
        ApplySoftDeleteQueryFilter(modelBuilder);
    }

    // Audit sahələrini avtomatik doldurur: YENİ obyektə CreatedDate, DƏYİŞMİŞ obyektə UpdatedDate.
    // Beləliklə bu tarixləri əl ilə təyin etmək lazım gəlmir.
    private void ApplyAuditFields()
    {
        DateTime now = DateTime.UtcNow;
        // ChangeTracker — bu kontekstdə dəyişdirilən bütün obyektləri izləyir.
        foreach (EntityEntry entry in ChangeTracker.Entries())
        {
            if (!InheritsFromEntity(entry.Entity.GetType()))
                continue;  // yalnız bizim Entity<> obyektləri (Identity tiplərini ötür)

            switch (entry.State)
            {
                case EntityState.Added:     // yeni əlavə olunur
                    entry.Property(nameof(Entity<int>.CreatedDate)).CurrentValue = now;
                    break;
                case EntityState.Modified:  // mövcud dəyişdirilir
                    entry.Property(nameof(Entity<int>.UpdatedDate)).CurrentValue = now;
                    break;
            }
        }
    }

    // SOFT-DELETE GLOBAL FİLTRİ: DeletedDate dolu olan (silinmiş) sətirlər BÜTÜN sorğularda
    // avtomatik gizlədilir — kod hər dəfə "WHERE DeletedDate IS NULL" yazmasın deyə.
    // Aşağıdakı kod hər entity üçün "e => e.DeletedDate == null" ifadəsini proqramla qurur.
    private static void ApplySoftDeleteQueryFilter(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!InheritsFromEntity(entityType.ClrType))
                continue;

            // "e" parametri → e.DeletedDate → (e.DeletedDate == null) → lambda hazırla
            ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "e");
            MemberExpression deletedDateProperty = Expression.Property(parameter, nameof(Entity<int>.DeletedDate));
            ConstantExpression nullValue = Expression.Constant(null, typeof(DateTime?));
            BinaryExpression isNotDeleted = Expression.Equal(deletedDateProperty, nullValue);
            LambdaExpression filter = Expression.Lambda(isNotDeleted, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }

    // Verilən tipin bizim Entity<> baza sinfindən miras alıb-almadığını yoxlayır.
    // (Identity tipləri Entity<>-dən gəlmir, ona görə onlara audit/soft-delete tətbiq olunmur.)
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
