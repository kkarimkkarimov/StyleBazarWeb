using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // Review entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// Review (rəy) entity-sinin bazaya map qaydaları (Fluent API). Diqqət: bir istifadəçi bir məhsula
// yalnız BİR rəy yaza bilsin deyə unikal indeks + reytinq 1–5 üçün CHECK məhdudiyyəti var.
public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        // İlkin açar = Id.
        builder.HasKey(r => r.Id);

        // Müəllif (UserId) məcburidir.
        builder.Property(r => r.UserId)
            .IsRequired();

        // Şərh maks. 2000 simvol (opsional).
        builder.Property(r => r.Comment)
            .HasMaxLength(2000);

        // Rəy → İstifadəçi əlaqəsi (1 User → N Review). Restrict → rəyi olan istifadəçini birbaşa silmə.
        builder.HasOne(r => r.User)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Rəy → Məhsul əlaqəsi (1 Product → N Review). Cascade → məhsul silinsə, rəyləri də silinir.
        builder.HasOne(r => r.Product)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // (UserId, ProductId) UNİKAL → bir istifadəçi eyni məhsula iki rəy yaza bilməz.
        builder.HasIndex(r => new { r.UserId, r.ProductId })
            .IsUnique();

        // Baza səviyyəsində CHECK məhdudiyyəti: Rating yalnız 1–5 arası ola bilər.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Reviews_Rating",
            "[Rating] >= 1 AND [Rating] <= 5"));
    }
}
