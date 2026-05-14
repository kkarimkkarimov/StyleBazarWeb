using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantity).IsRequired();

        builder.Property(i => i.UnitPriceSnapshot)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ProductSize)
            .WithMany()
            .HasForeignKey(i => i.ProductSizeId)
            .OnDelete(DeleteBehavior.SetNull);

        // A given product+size combo should only ever have one line per cart —
        // duplicate adds merge into the existing line.
        builder.HasIndex(i => new { i.CartId, i.ProductId, i.ProductSizeId })
            .IsUnique();
    }
}
