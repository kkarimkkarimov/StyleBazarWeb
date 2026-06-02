using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

// Wishlist (seçilmişlər) entity-sinin baza qaydaları. Bir istifadəçi eyni məhsulu
// iki dəfə seçilmişə əlavə edə bilməsin deyə (UserId, ProductId) üzərində unikal indeks var.
public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.HasKey(w => w.Id);   // primary key

        builder.Property(w => w.UserId)
            .IsRequired();

        // Seçimi edən istifadəçi.
        builder.HasOne(w => w.User)
            .WithMany(u => u.Wishlists)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seçilən məhsul.
        builder.HasOne(w => w.Product)
            .WithMany(p => p.Wishlists)
            .HasForeignKey(w => w.ProductId)
            .OnDelete(DeleteBehavior.Cascade);   // məhsul silinsə, seçilmiş qeydləri də silinir

        // Eyni istifadəçi + eyni məhsul cütü yalnız bir dəfə ola bilər.
        builder.HasIndex(w => new { w.UserId, w.ProductId })
            .IsUnique();
    }
}
