using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.UserId).IsRequired();
        builder.Property(o => o.OrderNumber).IsRequired().HasMaxLength(40);

        builder.Property(o => o.ShipFullName).IsRequired().HasMaxLength(150);
        builder.Property(o => o.ShipPhone).IsRequired().HasMaxLength(30);
        builder.Property(o => o.ShipCity).IsRequired().HasMaxLength(80);
        builder.Property(o => o.ShipAddressLine).IsRequired().HasMaxLength(500);
        builder.Property(o => o.ShipPostalCode).HasMaxLength(20);
        builder.Property(o => o.ShipNotes).HasMaxLength(500);
        builder.Property(o => o.CustomerNote).HasMaxLength(500);

        builder.Property(o => o.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(o => o.ShippingFee).HasColumnType("decimal(18,2)");
        builder.Property(o => o.Total).HasColumnType("decimal(18,2)");

        builder.HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasIndex(o => new { o.UserId, o.Status });
        builder.HasIndex(o => o.CreatedDate);
    }
}
