using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // ProductSize entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// ProductSize entity-sinin bazaya map qaydaları (Fluent API).
public class ProductSizeConfiguration : IEntityTypeConfiguration<ProductSize>
{
    public void Configure(EntityTypeBuilder<ProductSize> builder)
    {
        // İlkin açar = Id.
        builder.HasKey(ps => ps.Id);

        // Size: məcburi (NOT NULL), maksimum 20 simvol (nvarchar(20)).
        builder.Property(ps => ps.Size)
            .IsRequired()
            .HasMaxLength(20);

        // StockQuantity: default dəyər = 0.
        builder.Property(ps => ps.StockQuantity)
            .HasDefaultValue(0);

        // Ölçü → Məhsul əlaqəsi: bir məhsulun çox ölçüsü (1 Product → N ProductSize).
        builder.HasOne(ps => ps.Product)
            .WithMany(p => p.Sizes)
            .HasForeignKey(ps => ps.ProductId)
            .OnDelete(DeleteBehavior.Cascade);   // məhsul silinsə, ölçüləri də silinir
    }
}
