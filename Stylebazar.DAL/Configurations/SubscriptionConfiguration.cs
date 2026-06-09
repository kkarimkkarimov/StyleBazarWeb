using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // Subscription entity-si.
using Stylebazar.DAL.Entities.Enums;                    // SubscriptionStatus enum-u (filtrli indeksdə işlənir).

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// Subscription (abunəlik) entity-sinin bazaya map qaydaları (Fluent API).
public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        // İlkin açar = Id.
        builder.HasKey(s => s.Id);

        // UserId məcburi; Notes opsional (maks. 1000).
        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.Notes).HasMaxLength(1000);

        // UserId-də Cascade YOX, Restrict-dir, çünki: (a) SQL Server eyni cədvələ (AspNetUsers)
        // birdən çox kaskad yoluna icazə vermir (UserId + ApprovedByAdminId hər ikisi ora baxır);
        // (b) istifadəçi silinsə də abunəlik tarixçəsini audit jurnalı kimi saxlamaq istəyirik.
        builder.HasOne(s => s.User)
            .WithMany(u => u.Subscriptions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Təsdiqləyən admin — admin hesabı silinsə də tarixçə qalır (SetNull → ApprovedByAdminId null olur).
        builder.HasOne(s => s.ApprovedByAdmin)
            .WithMany()
            .HasForeignKey(s => s.ApprovedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        // Oxu nümunələri:
        //   1. "X istifadəçinin cari aktiv abunəliyi" — hər /Seller/* girişində
        //   2. "X-in bütün tarixçəsi, ən yeni öndə" — admin Detail səhifəsi
        builder.HasIndex(s => new { s.UserId, s.Status });
        builder.HasIndex(s => new { s.UserId, s.CreatedDate });

        // Filtrli UNİKAL indeks: hər istifadəçidə ən çoxu BİR Active abunəlik ola bilər.
        // HasFilter → indeks yalnız Status=Active VƏ DeletedDate IS NULL sətirlərə tətbiq olunur.
        builder.HasIndex(s => s.UserId)
            .IsUnique()
            .HasFilter($"[Status] = {(int)SubscriptionStatus.Active} AND [DeletedDate] IS NULL");
    }
}
