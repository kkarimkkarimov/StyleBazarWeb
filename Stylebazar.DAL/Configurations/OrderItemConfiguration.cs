using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.ProductImageUrl).HasMaxLength(500);
        builder.Property(i => i.SizeLabel).HasMaxLength(40);
        builder.Property(i => i.StoreName).IsRequired().HasMaxLength(100);

        builder.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(i => i.LineTotal).HasColumnType("decimal(18,2)");

        // NoAction (and not SetNull/Cascade) so we don't fight SQL Server's
        // "multiple cascade paths" guard — Product is referenced by Wishlist
        // (Cascade) and CartItem (Restrict). With NoAction here, deleting a
        // Product is simply blocked at the DB level when historical orders
        // reference it, which is the safe choice for an order log anyway.
        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(i => i.ProductSize)
            .WithMany()
            .HasForeignKey(i => i.ProductSizeId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
