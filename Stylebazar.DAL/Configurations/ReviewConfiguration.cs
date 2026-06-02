using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

// Review (rəy) entity-sinin baza qaydaları (Fluent API). Diqqət: bir istifadəçi bir
// məhsula yalnız BİR rəy yaza bilsin deyə unikal indeks + rating 1–5 üçün CHECK məhdudiyyəti var.
public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);   // primary key

        builder.Property(r => r.UserId)
            .IsRequired();           // müəllif məcburidir

        builder.Property(r => r.Comment)
            .HasMaxLength(2000);     // şərh maks. 2000 simvol

        // Rəyi yazan istifadəçi (1 User → N Review).
        builder.HasOne(r => r.User)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);   // rəyi olan istifadəçini birbaşa silmə

        // Rəyin aid olduğu məhsul (1 Product → N Review).
        builder.HasOne(r => r.Product)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);    // məhsul silinsə, rəyləri də silinir

        // Bir istifadəçi eyni məhsula iki rəy yaza bilməz (unikal cüt).
        builder.HasIndex(r => new { r.UserId, r.ProductId })
            .IsUnique();

        // Bazada məhdudiyyət: Rating yalnız 1–5 arası ola bilər.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Reviews_Rating",
            "[Rating] >= 1 AND [Rating] <= 5"));
    }
}
