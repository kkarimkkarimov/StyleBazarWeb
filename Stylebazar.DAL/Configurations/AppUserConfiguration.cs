using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

// AppUser (istifadəçi) entity-sinin əlavə sahələrinin baza qaydalarını təyin edir (Fluent API).
// Identity-nin öz sahələri avtomatik qurulur; burada yalnız BİZİM əlavə etdiyimiz sahələri ayarlayırıq.
public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.FullName)
            .IsRequired()           // ad-soyad məcburidir
            .HasMaxLength(100);     // maks. 100 simvol

        builder.Property(u => u.ProfileImageUrl)
            .HasMaxLength(500);     // şəkil URL-i üçün maks. uzunluq

        builder.Property(u => u.Bio)
            .HasMaxLength(1000);    // "haqqında" mətni üçün limit

        builder.Property(u => u.CreatedDate)
            .IsRequired();          // qeydiyyat tarixi mütləq olmalıdır
    }
}
