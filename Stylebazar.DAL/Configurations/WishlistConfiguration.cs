using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // Wishlist entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// Wishlist (seçilmişlər) entity-sinin bazaya map qaydaları. Bir istifadəçi eyni məhsulu iki dəfə
// seçilmişə əlavə edə bilməsin deyə (UserId, ProductId) üzərində unikal indeks var.
public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        // İlkin açar = Id.
        builder.HasKey(w => w.Id);

        // İstifadəçi id-si məcburidir.
        builder.Property(w => w.UserId)
            .IsRequired();

        // Seçim → İstifadəçi əlaqəsi. Restrict → seçimi olan istifadəçini birbaşa silmə.
        builder.HasOne(w => w.User)
            .WithMany(u => u.Wishlists)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seçim → Məhsul əlaqəsi. Cascade → məhsul silinsə, seçilmiş qeydləri də silinir.
        builder.HasOne(w => w.Product)
            .WithMany(p => p.Wishlists)
            .HasForeignKey(w => w.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // (UserId, ProductId) UNİKAL → eyni istifadəçi + eyni məhsul cütü yalnız bir dəfə.
        builder.HasIndex(w => new { w.UserId, w.ProductId })
            .IsUnique();
    }
}
