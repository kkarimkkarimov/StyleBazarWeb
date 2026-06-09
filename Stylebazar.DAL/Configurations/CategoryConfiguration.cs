using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // Category entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// Category entity-sinin bazaya map qaydaları (Fluent API): açar, sahə limitləri,
// özünə-istinad edən valideyn əlaqəsi və unikal slug indeksi.
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // İlkin açar = Id.
        builder.HasKey(c => c.Id);

        // Name: məcburi, maks. 100 simvol.
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Slug: məcburi, maks. 120 simvol.
        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(120);

        // Özünə-istinad əlaqəsi: kateqoriyanın valideyni ola bilər (ağac/iyerarxiya quruluşu).
        // OnDelete(Restrict) → alt-kateqoriyası olan valideyni silmək qadağandır.
        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.ChildCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Slug unikal indeks — hər kateqoriya URL-i təkdir (təkrarlanmasın).
        builder.HasIndex(c => c.Slug)
            .IsUnique();
    }
}
