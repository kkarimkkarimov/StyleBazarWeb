using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // MessageReaction entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// MessageReaction (mesaj reaksiyası) entity-sinin bazaya map qaydaları (Fluent API).
public class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> b)
    {
        // İlkin açar = Id.
        b.HasKey(x => x.Id);
        // UserId məcburi; Emoji qısa (bir emoji bir neçə UTF-16 kod vahidi tuta bilər).
        b.Property(x => x.UserId).IsRequired();
        b.Property(x => x.Emoji).IsRequired().HasMaxLength(16);

        // Reaksiya → Mesaj əlaqəsi. Cascade → mesaj (nadir halda) silinəndə reaksiyaları da silinir.
        b.HasOne(x => x.Message)
            .WithMany(m => m.Reactions)
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Reaksiya → İstifadəçi əlaqəsi. Restrict → istifadəçi silinməsi xüsusi təmizləmə yolu ilə gedir.
        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // (MessageId, UserId) UNİKAL → hər istifadəçi bir mesaja yalnız bir reaksiya qoya bilər.
        b.HasIndex(x => new { x.MessageId, x.UserId }).IsUnique();
    }
}
