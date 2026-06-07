using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

// Order (sifariş) entity-sinin baza qaydaları (Fluent API). Pul sahələri decimal(18,2).
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.BuyerId).IsRequired();
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Property(x => x.PaymentReference).HasMaxLength(200);

        b.Property(x => x.ShipFullName).IsRequired().HasMaxLength(100);
        b.Property(x => x.ShipPhone).IsRequired().HasMaxLength(40);
        b.Property(x => x.ShipCity).IsRequired().HasMaxLength(80);
        b.Property(x => x.ShipDistrict).HasMaxLength(80);
        b.Property(x => x.ShipAddressLine).IsRequired().HasMaxLength(300);
        b.Property(x => x.ShipPostalCode).HasMaxLength(16);
        b.Property(x => x.ShipNotes).HasMaxLength(300);

        // Restrict — sifariş maliyyə qeydidir, alıcı silinəndə kaskad silinməsin.
        b.HasOne(x => x.Buyer).WithMany().HasForeignKey(x => x.BuyerId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.BuyerId);
        b.HasIndex(x => x.Status);
    }
}
