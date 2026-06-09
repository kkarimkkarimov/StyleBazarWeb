using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // AppRole entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// AppRole (rol) entity-sinin konfiqurasiyası. Hələlik BOŞDUR, çünki Identity-nin default
// rol qurğusu kifayət edir. Gələcəkdə xüsusi qaydalar buraya əlavə oluna bilər.
public class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        // Default Identity konfiqurasiyası kifayətdir; gələcəkdə əlavə qaydalar buraya yazıla bilər.
    }
}
