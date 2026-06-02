using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

// ProductSize entity-sinin VERİLƏNLƏR BAZASINDA necə qurulacağını təyin edir (Fluent API).
// IEntityTypeConfiguration → EF bu sinifləri OnModelCreating-də avtomatik tapıb tətbiq edir.
public class ProductSizeConfiguration : IEntityTypeConfiguration<ProductSize>
{
    public void Configure(EntityTypeBuilder<ProductSize> builder)
    {
        builder.HasKey(ps => ps.Id);   // Primary key = Id

        builder.Property(ps => ps.Size)
            .IsRequired()               // boş ola bilməz (NOT NULL)
            .HasMaxLength(20);          // maksimum 20 simvol (məs. nvarchar(20))

        builder.Property(ps => ps.StockQuantity)
            .HasDefaultValue(0);        // default dəyər = 0

        // Əlaqə: bir Məhsulun ÇOX ölçüsü olur (1 Product → N ProductSize).
        builder.HasOne(ps => ps.Product)          // hər ölçünün 1 məhsulu var
            .WithMany(p => p.Sizes)               // məhsulun çox ölçüsü var
            .HasForeignKey(ps => ps.ProductId)    // əlaqə açarı (foreign key)
            .OnDelete(DeleteBehavior.Cascade);    // məhsul silinsə, ölçüləri də silinir
    }
}
