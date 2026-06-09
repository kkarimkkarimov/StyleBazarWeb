using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri (HasKey, Property, HasIndex...).
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // Product entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// Product entity-sinin verilənlər bazasına NECƏ map olunduğunu təyin edir (Fluent API).
// IEntityTypeConfiguration<Product> → EF model qurarkən bu qaydaları avtomatik tətbiq edir.
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    // Bütün map qaydaları "builder" üzərində qurulur.
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // İlkin açar (primary key) = Id sütunu.
        builder.HasKey(p => p.Id);

        // Name: məcburi (NOT NULL), maksimum 200 simvol.
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Slug: məcburi, maksimum 220 simvol.
        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(220);

        // Description: məcburi, maksimum 4000 simvol.
        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(4000);

        // Price: pul sahəsi → decimal(18,2) (18 rəqəm, 2 onluq).
        builder.Property(p => p.Price)
            .HasPrecision(18, 2);

        // DiscountPrice: pul sahəsi → decimal(18,2).
        builder.Property(p => p.DiscountPrice)
            .HasPrecision(18, 2);

        // Brand: maksimum 100 simvol (opsional sahə).
        builder.Property(p => p.Brand).HasMaxLength(100);

        // Sərbəst mətnli xüsusiyyətlər — hamısı opsional, qısa limitlərlə (sağ panel səliqəli qalsın).
        builder.Property(p => p.Material).HasMaxLength(80);
        builder.Property(p => p.Pattern).HasMaxLength(80);
        builder.Property(p => p.Neckline).HasMaxLength(80);
        builder.Property(p => p.SleeveLength).HasMaxLength(80);
        builder.Property(p => p.Fit).HasMaxLength(80);
        builder.Property(p => p.Season).HasMaxLength(80);
        builder.Property(p => p.Style).HasMaxLength(80);
        builder.Property(p => p.CountryOfOrigin).HasMaxLength(80);

        // Məhsul → Mağaza əlaqəsi: bir məhsulun bir mağazası, bir mağazanın çox məhsulu (HasOne/WithMany).
        // OnDelete(Restrict) → məhsulu olan mağazanı silmək olmaz (kaskad silinmənin qarşısını alır).
        builder.HasOne(p => p.Store)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        // Məhsul → Kateqoriya əlaqəsi: bir məhsulun bir kateqoriyası var.
        // OnDelete(Restrict) → məhsulu olan kateqoriyanı silmək olmaz.
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // İndekslər — tez-tez filtr/axtarış olunan sütunlar üçün sorğu sürətini artırır.
        builder.HasIndex(p => p.StoreId);
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.IsFeatured);
        builder.HasIndex(p => p.Slug);
    }
}
