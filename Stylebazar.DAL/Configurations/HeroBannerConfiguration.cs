using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

public class HeroBannerConfiguration : IEntityTypeConfiguration<HeroBanner>
{
    public void Configure(EntityTypeBuilder<HeroBanner> builder)
    {
        builder.Property(h => h.Title).IsRequired().HasMaxLength(200);
        builder.Property(h => h.Subtitle).HasMaxLength(500);
        builder.Property(h => h.Eyebrow).HasMaxLength(100);
        builder.Property(h => h.ImageUrl).HasMaxLength(500);
        builder.Property(h => h.CtaText).HasMaxLength(100);
        builder.Property(h => h.CtaUrl).HasMaxLength(500);

        builder.HasIndex(h => new { h.IsActive, h.DisplayOrder });
    }
}
