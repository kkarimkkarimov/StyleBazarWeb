using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

public class StoreFollowerConfiguration : IEntityTypeConfiguration<StoreFollower>
{
    public void Configure(EntityTypeBuilder<StoreFollower> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.UserId).IsRequired();

        builder.HasOne(f => f.User)
            .WithMany(u => u.StoreFollows)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Store)
            .WithMany(s => s.Followers)
            .HasForeignKey(f => f.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        // One row per (user, store) pair — UNIQUE so the count == distinct users.
        builder.HasIndex(f => new { f.UserId, f.StoreId }).IsUnique();
    }
}
