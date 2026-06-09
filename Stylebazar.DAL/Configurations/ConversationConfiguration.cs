using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // Conversation entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// Conversation (söhbət) entity-sinin bazaya map qaydaları (Fluent API).
public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        // İki iştirakçı id-si məcburi (Identity id-ləri 450 simvola qədər); önizləmə qısa.
        builder.Property(c => c.User1Id).IsRequired().HasMaxLength(450);
        builder.Property(c => c.User2Id).IsRequired().HasMaxLength(450);
        builder.Property(c => c.LastMessagePreview).HasMaxLength(120);

        // (User1Id, User2Id) UNİKAL — ChatManager-dəki əlifba sırası ilə birlikdə hər cüt üçün BİR sətir.
        builder.HasIndex(c => new { c.User1Id, c.User2Id }).IsUnique();
        // Gələnlər qutusu hər iştirakçı üçün LastMessageAt-a görə (azalan) sıralanır.
        builder.HasIndex(c => new { c.User1Id, c.LastMessageAt });
        builder.HasIndex(c => new { c.User2Id, c.LastMessageAt });

        // AspNetUsers-ə FK-lar Restrict-dir — istifadəçinin silinməsi AccountManager.DeleteUserAsync
        // vasitəsilə gedir, o, söhbətləri ayrıca təmizləyir.
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
