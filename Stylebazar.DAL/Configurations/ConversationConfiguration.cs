using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.Property(c => c.User1Id).IsRequired().HasMaxLength(450);
        builder.Property(c => c.User2Id).IsRequired().HasMaxLength(450);
        builder.Property(c => c.LastMessagePreview).HasMaxLength(120);

        // Unique on (User1Id, User2Id) — combined with the alphabetical-sort
        // invariant in ChatManager.GetOrCreate, this guarantees ONE row per pair.
        builder.HasIndex(c => new { c.User1Id, c.User2Id }).IsUnique();
        // Inbox-list query orders by LastMessageAt desc per participant.
        builder.HasIndex(c => new { c.User1Id, c.LastMessageAt });
        builder.HasIndex(c => new { c.User2Id, c.LastMessageAt });

        // FKs to AspNetUsers are Restrict — deleting a user goes through
        // AccountManager.DeleteUserAsync which cleans up conversations
        // explicitly (Phase 14.2+).
        builder.HasOne(c => c.User1)
            .WithMany()
            .HasForeignKey(c => c.User1Id)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.User2)
            .WithMany()
            .HasForeignKey(c => c.User2Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
