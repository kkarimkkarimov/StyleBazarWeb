using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // OrderItem entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// OrderItem (sifariş sətri) entity-sinin bazaya map qaydaları (Fluent API). Snapshot sahələri + decimal(18,2).
public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        // İlkin açar = Id.
        b.HasKey(x => x.Id);
        // Snapshot sahələri: ad məcburi; slug/şəkil/ölçü/rəng opsional; vahid qiymət decimal(18,2).
        b.Property(x => x.ProductName).IsRequired().HasMaxLength(200);
        b.Property(x => x.ProductSlug).HasMaxLength(200);
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.Size).HasMaxLength(50);
        b.Property(x => x.Color).HasMaxLength(50);

        // LineTotal hesablanan xassədir (UnitPrice × Quantity) → Ignore ilə DB sütunu yaradılmır.
        b.Ignore(x => x.LineTotal);

        // Sifariş → sətirlər: Cascade (sifariş silinsə sətirləri də silinir).
        // Məhsul/Mağaza → Restrict (soft-delete-li, snapshot sətri tarixçə üçün qalır).
        b.HasOne(x => x.Order).WithMany(o => o.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);

        // StoreId indeksi — satıcı öz sifarişlərini tez tapsın.
        b.HasIndex(x => x.StoreId);
    }
}
