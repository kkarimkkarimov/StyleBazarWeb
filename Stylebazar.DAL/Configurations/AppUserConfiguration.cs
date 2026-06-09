using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // AppUser entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// AppUser (istifadəçi) entity-sinin ƏLAVƏ sahələrinin map qaydaları (Fluent API).
// Identity-nin öz sahələri (Email, PasswordHash...) avtomatik qurulur; burada yalnız bizim
// əlavə etdiyimiz sahələri ayarlayırıq.
public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        // FullName: məcburi, maks. 100 simvol.
        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(100);

        // ProfileImageUrl: opsional, maks. 500 simvol.
        builder.Property(u => u.ProfileImageUrl)
            .HasMaxLength(500);

        // Bio: opsional "haqqında" mətni, maks. 1000 simvol.
        builder.Property(u => u.Bio)
            .HasMaxLength(1000);

        // CreatedDate: qeydiyyat tarixi məcburidir.
        builder.Property(u => u.CreatedDate)
            .IsRequired();
    }
}
