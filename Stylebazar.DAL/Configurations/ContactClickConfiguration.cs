using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // ContactClick entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// ContactClick (əlaqə kliki) entity-sinin bazaya map qaydaları (Fluent API).
public class ContactClickConfiguration : IEntityTypeConfiguration<ContactClick>
{
    public void Configure(EntityTypeBuilder<ContactClick> builder)
    {
        // İlkin açar = Id.
        builder.HasKey(c => c.Id);

        // IP / User-Agent / Referer — hamısı opsional, limitli.
        builder.Property(c => c.IpAddress).HasMaxLength(64);
        builder.Property(c => c.UserAgent).HasMaxLength(500);
        builder.Property(c => c.Referer).HasMaxLength(500);

        // Klik → Mağaza əlaqəsi. Restrict → kliki olan mağazanı silmək olmaz.
        builder.HasOne(c => c.Store)
            .WithMany()
            .HasForeignKey(c => c.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        // Klik → Məhsul əlaqəsi (opsional FK). Restrict → məhsulu silmək olmaz.
        builder.HasOne(c => c.Product)
            .WithMany()
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Klik → İstifadəçi əlaqəsi. SetNull → istifadəçi silinsə klik qalır, UserId null olur (anonimləşir).
        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // İndekslər satıcı analitika panelini qidalandırır:
        //   1. Mağaza + tarix — "mağazama günlük kliklər"
        //   2. Məhsul + tarix — "ən çox kliklənən məhsullar"
        //   3. Əlaqə növü — "WhatsApp vs telefon bölgüsü"
        builder.HasIndex(c => new { c.StoreId, c.CreatedDate });
        builder.HasIndex(c => new { c.ProductId, c.CreatedDate });
        builder.HasIndex(c => c.ContactType);
    }
}
