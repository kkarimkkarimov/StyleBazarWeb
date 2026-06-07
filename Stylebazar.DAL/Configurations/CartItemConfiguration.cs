using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

// CartItem entity-sinin baza qaydaları (Fluent API).
public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.UserId).IsRequired();
        b.Property(x => x.Quantity).HasDefaultValue(1);
        b.Property(x => x.Size).HasMaxLength(50);
        b.Property(x => x.Color).HasMaxLength(50);

        // İstifadəçi silinsə → səbəti təmizlənir; məhsul silinsə → səbətdən çıxır.
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);

        // (UserId, ProductId) unikal DEYİL — eyni məhsul fərqli ölçü/rənglə ayrı sətir ola bilər.
        b.HasIndex(x => x.UserId);
    }
}
