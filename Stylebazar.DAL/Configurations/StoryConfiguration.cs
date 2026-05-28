using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

public class StoryConfiguration : IEntityTypeConfiguration<Story>
{
    public void Configure(EntityTypeBuilder<Story> builder)
    {
        builder.Property(s => s.MediaUrl).IsRequired().HasMaxLength(500);
        builder.Property(s => s.ThumbnailUrl).HasMaxLength(500);
        // Stored as int (the enum's underlying type) — EF default for enums.
        builder.Property(s => s.MediaType).HasConversion<int>();
        builder.Property(s => s.Caption).HasMaxLength(500);
        builder.Property(s => s.LinkUrl).HasMaxLength(500);

        builder.HasIndex(s => s.StoreId);
        builder.HasIndex(s => new { s.IsActive, s.ExpiresAt });

        builder.HasOne(s => s.Store)
            .WithMany()
            .HasForeignKey(s => s.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
