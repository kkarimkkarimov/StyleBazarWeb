using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // StoreFollower entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// StoreFollower (mağaza izləyicisi) entity-sinin bazaya map qaydaları (Fluent API).
public class StoreFollowerConfiguration : IEntityTypeConfiguration<StoreFollower>
{
    public void Configure(EntityTypeBuilder<StoreFollower> builder)
    {
        // İlkin açar = Id.
        builder.HasKey(f => f.Id);

        // İzləyənin id-si məcburidir.
        builder.Property(f => f.UserId).IsRequired();

        // İzləyici → İstifadəçi əlaqəsi. Cascade → istifadəçi silinəndə izləmələri silinir.
        builder.HasOne(f => f.User)
            .WithMany(u => u.StoreFollows)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // İzləyici → Mağaza əlaqəsi. Cascade → mağaza silinəndə izləyici qeydləri silinir.
        builder.HasOne(f => f.Store)
            .WithMany(s => s.Followers)
            .HasForeignKey(f => f.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        // (UserId, StoreId) UNİKAL indeks → eyni cüt bir dəfə; say = fərqli izləyicilərin sayı.
        builder.HasIndex(f => new { f.UserId, f.StoreId }).IsUnique();
    }
}
