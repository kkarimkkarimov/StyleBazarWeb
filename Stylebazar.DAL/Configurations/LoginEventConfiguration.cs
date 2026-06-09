using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // LoginEvent entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// LoginEvent entity-sinin bazaya map qaydaları (Fluent API).
public class LoginEventConfiguration : IEntityTypeConfiguration<LoginEvent>
{
    public void Configure(EntityTypeBuilder<LoginEvent> builder)
    {
        // İlkin açar = Id.
        builder.HasKey(e => e.Id);

        // UserId məcburi; IP və User-Agent opsional (limitli).
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.IpAddress).HasMaxLength(64);
        builder.Property(e => e.UserAgent).HasMaxLength(500);

        // Hadisə → İstifadəçi əlaqəsi (WithMany() naviqasiyasız tərəf).
        // OnDelete(Cascade) → istifadəçi silinəndə giriş tarixçəsi də silinir.
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Kompozit indeks (UserId + CreatedDate) → "giriş tarixçəm"i tez gətirir.
        builder.HasIndex(e => new { e.UserId, e.CreatedDate });
    }
}
