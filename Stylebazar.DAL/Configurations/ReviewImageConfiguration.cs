using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // ReviewImage entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// ReviewImage (rəy şəkli) entity-sinin bazaya map qaydaları (Fluent API).
public class ReviewImageConfiguration : IEntityTypeConfiguration<ReviewImage>
{
    public void Configure(EntityTypeBuilder<ReviewImage> builder)
    {
        // İlkin açar = Id.
        builder.HasKey(ri => ri.Id);

        // ImageUrl: məcburi, maks. 500 simvol.
        builder.Property(ri => ri.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        // Şəkil → Rəy əlaqəsi: bir rəyin çox şəkli (1 Review → N ReviewImage).
        builder.HasOne(ri => ri.Review)
            .WithMany(r => r.Images)
            .HasForeignKey(ri => ri.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);   // rəy silinsə, şəkilləri də silinir
    }
}
