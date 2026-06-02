using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

// Category entity-sinin baza qaydaları (Fluent API): açar, sahə limitləri,
// özünə-istinad edən valideyn əlaqəsi və unikal slug indeksi.
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);   // primary key

        builder.Property(c => c.Name)
            .IsRequired()            // ad məcburidir
            .HasMaxLength(100);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(120);

        // Özünə-istinad: kateqoriyanın valideyni ola bilər (ağac/iyerarxiya quruluşu).
        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.ChildCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);   // alt-kateqoriyası varsa valideyni silmək qadağandır

        builder.HasIndex(c => c.Slug)
            .IsUnique();             // hər slug unikaldır (URL təkrarlanmasın)
    }
}
