using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // ContactMessage entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// ContactMessage entity-sinin bazaya map qaydaları (Fluent API).
public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        // Sahə limitləri: ad/email/mesaj məcburi, mövzu/qeyd/IP opsional.
        builder.Property(c => c.FullName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Subject).HasMaxLength(200);
        builder.Property(c => c.Message).IsRequired().HasMaxLength(4000);
        builder.Property(c => c.AdminNote).HasMaxLength(2000);
        builder.Property(c => c.UserIpAddress).HasMaxLength(64);

        // İndekslər: statusa görə filtr (admin "Yeni" mesajlar) + tarixə görə sıralama.
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.CreatedDate);
    }
}
