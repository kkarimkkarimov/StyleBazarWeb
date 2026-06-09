using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // Message entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// Message (mesaj) entity-sinin bazaya map qaydaları (Fluent API).
public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        // Göndərən id məcburi; mətn/əlavə opsional; növ int kimi saxlanır.
        builder.Property(m => m.SenderId).IsRequired().HasMaxLength(450);
        builder.Property(m => m.Body).HasMaxLength(2000);
        builder.Property(m => m.AttachmentUrl).HasMaxLength(500);
        builder.Property(m => m.MessageType).HasConversion<int>();

        // Ən çox işlənən sorğu: "X söhbətinin son N mesajını tarixə görə azalan gətir" → buna görə indeks.
        builder.HasIndex(m => new { m.ConversationId, m.CreatedDate });

        // Mesaj → Söhbət əlaqəsi. Cascade → söhbət silinəndə (nadir, yalnız admin) mesajları silinir.
        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Mesaj → Göndərən əlaqəsi. Restrict → göndərəni birbaşa silmək olmaz.
        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // StoryId / ProductId opsional istinadlardır — SetNull ilə istinad edilən hekayə/məhsul
        // silinsə, köhnə mesaj qalır, sadəcə istinad null olur.
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
