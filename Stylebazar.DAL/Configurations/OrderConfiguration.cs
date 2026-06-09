using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // Order entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// Order (sifariş) entity-sinin bazaya map qaydaları (Fluent API). Pul sahəsi decimal(18,2).
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        // İlkin açar = Id.
        b.HasKey(x => x.Id);
        // Alıcı id məcburi; cəmi məbləğ decimal(18,2); ödəniş istinadı limitli.
        b.Property(x => x.BuyerId).IsRequired();
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Property(x => x.PaymentReference).HasMaxLength(200);

        // Çatdırılma snapshot sahələri — məcburilər (ad/telefon/şəhər/ünvan) + opsionallar.
        b.Property(x => x.ShipFullName).IsRequired().HasMaxLength(100);
        b.Property(x => x.ShipPhone).IsRequired().HasMaxLength(40);
        b.Property(x => x.ShipCity).IsRequired().HasMaxLength(80);
        b.Property(x => x.ShipDistrict).HasMaxLength(80);
        b.Property(x => x.ShipAddressLine).IsRequired().HasMaxLength(300);
        b.Property(x => x.ShipPostalCode).HasMaxLength(16);
        b.Property(x => x.ShipNotes).HasMaxLength(300);

        // Sifariş → Alıcı əlaqəsi. Restrict → sifariş maliyyə qeydidir, alıcı silinəndə kaskad silinməsin.
        b.HasOne(x => x.Buyer).WithMany().HasForeignKey(x => x.BuyerId).OnDelete(DeleteBehavior.Restrict);

        // İndekslər: alıcıya görə (sifariş tarixçəm) + statusa görə (admin filtri).
        b.HasIndex(x => x.BuyerId);
        b.HasIndex(x => x.Status);
    }
}
