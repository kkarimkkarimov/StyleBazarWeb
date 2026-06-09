using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // ProductColor entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// ProductColor entity-sinin bazaya map qaydaları (Fluent API).
public class ProductColorConfiguration : IEntityTypeConfiguration<ProductColor>
{
    public void Configure(EntityTypeBuilder<ProductColor> builder)
    {
        // İlkin açar = Id.
        builder.HasKey(c => c.Id);

        // Name (maks. 40) və Hex (maks. 10) məcburidir.
        builder.Property(c => c.Name).IsRequired().HasMaxLength(40);
        builder.Property(c => c.Hex).IsRequired().HasMaxLength(10);

        // Rəng → Məhsul əlaqəsi. OnDelete(Cascade) → məhsul silinəndə rəngləri də avtomatik silinir.
        builder.HasOne(c => c.Product)
            .WithMany(p => p.Colors)
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Məhsula görə rəngləri tez gətirmək üçün indeks.
        builder.HasIndex(c => c.ProductId);
    }
}
