using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.Property(m => m.SenderId).IsRequired().HasMaxLength(450);
        builder.Property(m => m.Body).HasMaxLength(2000);
        builder.Property(m => m.AttachmentUrl).HasMaxLength(500);
        builder.Property(m => m.MessageType).HasConversion<int>();

        // The hot query: "give me the latest N messages in conversation X
        // ordered by date desc". Indexed accordingly.
        builder.HasIndex(m => new { m.ConversationId, m.CreatedDate });

        // Conversation cascade — when the conversation row goes (rare; only
        // admin moderation), drop its messages.
        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // StoryId / ProductId are nullable references — SetNull so deleting
        // the referenced story/product doesn't blow up old reply messages.
        builder.HasOne(m => m.Story)
            .WithMany()
            .HasForeignKey(m => m.StoryId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(m => m.Product)
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
