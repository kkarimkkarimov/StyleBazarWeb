using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // CartItem entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// CartItem (səbət elementi) entity-sinin bazaya map qaydaları (Fluent API).
public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> b)
    {
        // İlkin açar = Id.
        b.HasKey(x => x.Id);
        // UserId məcburi; Quantity default 1; ölçü/rəng qısa mətn.
        b.Property(x => x.UserId).IsRequired();
        b.Property(x => x.Quantity).HasDefaultValue(1);
        b.Property(x => x.Size).HasMaxLength(50);
        b.Property(x => x.Color).HasMaxLength(50);

        // İstifadəçi silinsə → səbəti təmizlənir (Cascade); məhsul silinsə → səbətdən çıxır (Cascade).
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);

        // Yalnız UserId indeksi — (UserId, ProductId) UNİKAL DEYİL, çünki eyni məhsul fərqli
        // ölçü/rənglə ayrı sətir kimi durur.
        b.HasIndex(x => x.UserId);
    }
}
