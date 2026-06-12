using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // QuickReply entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// QuickReply (sürətli cavab şablonu) entity-sinin bazaya map qaydaları (Fluent API).
public class QuickReplyConfiguration : IEntityTypeConfiguration<QuickReply>
{
    public void Configure(EntityTypeBuilder<QuickReply> b)
    {
        // İlkin açar = Id.
        b.HasKey(x => x.Id);
        // Sahənin limitləri: başlıq qısa, mətn mesaj limiti ilə eyni miqyasda.
        b.Property(x => x.UserId).IsRequired().HasMaxLength(450);
        b.Property(x => x.Title).IsRequired().HasMaxLength(60);
        b.Property(x => x.Body).IsRequired().HasMaxLength(1000);

        // Şablon → Sahib əlaqəsi. Cascade → istifadəçi silinəndə şablonları da silinir.
        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // "Mənim şablonlarım" sorğusu üçün indeks.
        b.HasIndex(x => x.UserId);
    }
}
