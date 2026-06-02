using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

// ReviewImage entity-sinin baza qaydaları (Fluent API).
public class ReviewImageConfiguration : IEntityTypeConfiguration<ReviewImage>
{
    public void Configure(EntityTypeBuilder<ReviewImage> builder)
    {
        builder.HasKey(ri => ri.Id);   // primary key

        builder.Property(ri => ri.ImageUrl)
            .IsRequired()              // şəkil URL-i məcburidir
            .HasMaxLength(500);

        // Bir rəyin ÇOX şəkli olur (1 Review → N ReviewImage).
        builder.HasOne(ri => ri.Review)
            .WithMany(r => r.Images)
            .HasForeignKey(ri => ri.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);   // rəy silinsə, şəkilləri də silinir
    }
}
