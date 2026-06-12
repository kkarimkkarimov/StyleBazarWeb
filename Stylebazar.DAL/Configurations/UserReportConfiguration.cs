using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // UserReport entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// UserReport (istifadəçi şikayəti) entity-sinin bazaya map qaydaları (Fluent API).
public class UserReportConfiguration : IEntityTypeConfiguration<UserReport>
{
    public void Configure(EntityTypeBuilder<UserReport> b)
    {
        // İlkin açar = Id.
        b.HasKey(x => x.Id);
        // İd-lər məcburi; səbəb mətni limitli.
        b.Property(x => x.ReporterId).IsRequired().HasMaxLength(450);
        b.Property(x => x.ReportedUserId).IsRequired().HasMaxLength(450);
        b.Property(x => x.Reason).IsRequired().HasMaxLength(1000);

        // Hər iki istifadəçi FK-sı Restrict (eyni cədvələ çoxlu kaskad qadağası + audit qorunması).
        b.HasOne(x => x.Reporter)
            .WithMany()
            .HasForeignKey(x => x.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReportedUser)
            .WithMany()
            .HasForeignKey(x => x.ReportedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Söhbət silinsə şikayət qalır, istinad null olur (SetNull).
        b.HasOne(x => x.Conversation)
            .WithMany()
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Admin "açıq şikayətlər, ən yenisi öndə" sorğusu üçün indeks.
        b.HasIndex(x => new { x.IsResolved, x.CreatedDate });
    }
}
