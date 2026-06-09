using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // Story entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// Story (hekayə) entity-sinin bazaya map qaydaları (Fluent API).
public class StoryConfiguration : IEntityTypeConfiguration<Story>
{
    public void Configure(EntityTypeBuilder<Story> builder)
    {
        // MediaUrl məcburi; ThumbnailUrl/Caption/LinkUrl opsional.
        builder.Property(s => s.MediaUrl).IsRequired().HasMaxLength(500);
        builder.Property(s => s.ThumbnailUrl).HasMaxLength(500);
        // MediaType enum-u int kimi saxlanır (enum-un əsas tipi) — EF-in standart davranışı.
        builder.Property(s => s.MediaType).HasConversion<int>();
        builder.Property(s => s.Caption).HasMaxLength(500);
        builder.Property(s => s.LinkUrl).HasMaxLength(500);

        // İndekslər: mağazaya görə hekayələr + (aktivdir + bitmə vaxtı) — "aktiv, bitməmiş hekayələr".
        builder.HasIndex(s => s.StoreId);
        builder.HasIndex(s => new { s.IsActive, s.ExpiresAt });

        // Hekayə → Mağaza əlaqəsi. Cascade → mağaza silinəndə hekayələri də silinir.
        builder.HasOne(s => s.Store)
            .WithMany()
            .HasForeignKey(s => s.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
