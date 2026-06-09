using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // Address entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// Address entity-sinin bazaya map qaydaları (Fluent API).
public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        // İlkin açar = Id.
        builder.HasKey(a => a.Id);

        // Məcburi sahələr (UserId/Title/FullName/Phone/City/AddressLine) + opsionallar (District/PostalCode/Notes).
        builder.Property(a => a.UserId).IsRequired();
        builder.Property(a => a.Title).IsRequired().HasMaxLength(60);
        builder.Property(a => a.FullName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Phone).IsRequired().HasMaxLength(30);
        builder.Property(a => a.City).IsRequired().HasMaxLength(80);
        builder.Property(a => a.District).HasMaxLength(80);
        builder.Property(a => a.AddressLine).IsRequired().HasMaxLength(300);
        builder.Property(a => a.PostalCode).HasMaxLength(16);
        builder.Property(a => a.Notes).HasMaxLength(300);

        // Ünvan → İstifadəçi əlaqəsi. OnDelete(Cascade) → istifadəçi silinəndə ünvanları da silinir.
        builder.HasOne(a => a.User)
            .WithMany(u => u.Addresses)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // İndekslər: ünvanlar istifadəçi başına, "əvvəl default, sonra ən yeni" sırası ilə gətirilir.
        builder.HasIndex(a => new { a.UserId, a.IsDefault });
        builder.HasIndex(a => new { a.UserId, a.CreatedDate });
    }
}
