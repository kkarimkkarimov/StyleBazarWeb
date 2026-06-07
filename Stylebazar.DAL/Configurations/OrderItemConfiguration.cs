using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

// OrderItem (sifariş sətri) entity-sinin baza qaydaları (Fluent API). Snapshot sahələri + decimal(18,2).
public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.ProductName).IsRequired().HasMaxLength(200);
        b.Property(x => x.ProductSlug).HasMaxLength(200);
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.Size).HasMaxLength(50);
        b.Property(x => x.Color).HasMaxLength(50);

        // LineTotal hesablanan property-dir (UnitPrice*Quantity) — DB-də sütun olmamalıdır.
        b.Ignore(x => x.LineTotal);

        // Sifariş silinsə → sətirləri də silinir. Məhsul/mağaza Restrict (soft-delete-li, sətir qalır).
        b.HasOne(x => x.Order).WithMany(o => o.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.StoreId);   // satıcı öz sifarişlərini tez tapsın
    }
}
