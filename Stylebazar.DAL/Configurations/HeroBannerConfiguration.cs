using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // HeroBanner entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// HeroBanner entity-sinin bazaya map qaydaları (Fluent API).
public class HeroBannerConfiguration : IEntityTypeConfiguration<HeroBanner>
{
    public void Configure(EntityTypeBuilder<HeroBanner> builder)
    {
        // Title məcburi (maks. 200); qalan mətn sahələri opsionaldır.
        builder.Property(h => h.Title).IsRequired().HasMaxLength(200);
        builder.Property(h => h.Subtitle).HasMaxLength(500);
        builder.Property(h => h.Eyebrow).HasMaxLength(100);
        builder.Property(h => h.ImageUrl).HasMaxLength(500);
        builder.Property(h => h.CtaText).HasMaxLength(100);
        builder.Property(h => h.CtaUrl).HasMaxLength(500);

        // Kompozit indeks (IsActive + DisplayOrder) → "aktiv bannerləri sıraya görə gətir" sorğusu üçün.
        builder.HasIndex(h => new { h.IsActive, h.DisplayOrder });
    }
}
