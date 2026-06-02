using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

// ProductImage entity-sinin baza qaydaları (Fluent API).
public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasKey(pi => pi.Id);   // primary key

        builder.Property(pi => pi.ImageUrl)
            .IsRequired()              // şəkil URL-i məcburidir
            .HasMaxLength(500);

        builder.Property(pi => pi.DisplayOrder)
            .HasDefaultValue(0);       // default sıra = 0

        // Bir məhsulun ÇOX şəkli olur (1 Product → N ProductImage).
        builder.HasOne(pi => pi.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);   // məhsul silinsə, şəkilləri də silinir
    }
}
