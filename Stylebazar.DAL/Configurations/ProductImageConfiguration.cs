using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // ProductImage entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// ProductImage (məhsul şəkli) entity-sinin bazaya map qaydaları (Fluent API).
public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        // İlkin açar = Id.
        builder.HasKey(pi => pi.Id);

        // ImageUrl: məcburi, maks. 500 simvol.
        builder.Property(pi => pi.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        // DisplayOrder: default sıra = 0.
        builder.Property(pi => pi.DisplayOrder)
            .HasDefaultValue(0);

        // Şəkil → Məhsul əlaqəsi: bir məhsulun çox şəkli (1 Product → N ProductImage).
        builder.HasOne(pi => pi.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);   // məhsul silinsə, şəkilləri də silinir
    }
}
