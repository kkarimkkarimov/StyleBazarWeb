using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

// AppRole (rol) entity-sinin baza konfiqurasiyası. Hələlik boşdur, çünki Identity-nin
// default rol qurğusu kifayət edir. Gələcəkdə xüsusi qaydalar buraya əlavə oluna bilər.
public class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        // Default Identity konfiqurasiyası kifayətdir.
        // Gələcəkdə əlavə qaydalar buraya yazıla bilər.
    }
}
