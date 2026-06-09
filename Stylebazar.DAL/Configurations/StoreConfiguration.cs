using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // Store entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// Store entity-sinin bazaya map qaydaları (Fluent API): açar, sahə limitləri, sahib əlaqəsi, indekslər.
public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        // İlkin açar = Id.
        builder.HasKey(s => s.Id);

        // Name: məcburi, maks. 100 simvol.
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Slug: məcburi, maks. 120 simvol.
        builder.Property(s => s.Slug)
            .IsRequired()
            .HasMaxLength(120);

        // Description: opsional, maks. 1000 simvol.
        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        // ProfileImageUrl: məcburi, maks. 500 simvol.
        builder.Property(s => s.ProfileImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        // CoverImageUrl: opsional, maks. 500 simvol.
        builder.Property(s => s.CoverImageUrl)
            .HasMaxLength(500);

        // ContactPhone: opsional, maks. 20 simvol.
        builder.Property(s => s.ContactPhone)
            .HasMaxLength(20);

        // Address: opsional, maks. 500 simvol.
        builder.Property(s => s.Address)
            .HasMaxLength(500);

        // Sosial əlaqə kanalları — hamısı opsional, qısa limitlərlə.
        builder.Property(s => s.WhatsAppNumber).HasMaxLength(30);
        builder.Property(s => s.InstagramHandle).HasMaxLength(60);
        builder.Property(s => s.TelegramHandle).HasMaxLength(60);
        builder.Property(s => s.FacebookHandle).HasMaxLength(60);
        builder.Property(s => s.TikTokHandle).HasMaxLength(60);

        // OwnerId: məcburi (mağazanın mütləq sahibi olmalıdır).
        builder.Property(s => s.OwnerId)
            .IsRequired();

        // Mağaza → Sahib (AppUser) əlaqəsi: bir mağazanın bir sahibi, bir istifadəçinin çox mağazası.
        // OnDelete(Restrict) → mağazası olan istifadəçini silmək olmaz.
        builder.HasOne(s => s.Owner)
            .WithMany(u => u.Stores)
            .HasForeignKey(s => s.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Slug unikal indeks — hər mağaza URL-i təkdir.
        builder.HasIndex(s => s.Slug)
            .IsUnique();

        // OwnerId indeksi — "mənim mağazalarım" sorğularını sürətləndirir.
        builder.HasIndex(s => s.OwnerId);
    }
}
