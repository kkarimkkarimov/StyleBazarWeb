using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

public class StoryConfiguration : IEntityTypeConfiguration<Story>
{
    public void Configure(EntityTypeBuilder<Story> builder)
    {
        builder.Property(s => s.ImageUrl).IsRequired().HasMaxLength(500);
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
