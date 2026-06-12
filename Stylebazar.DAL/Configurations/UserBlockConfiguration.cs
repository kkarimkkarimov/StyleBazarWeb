using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // UserBlock entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// UserBlock (istifadəçi bloku) entity-sinin bazaya map qaydaları (Fluent API).
public class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> b)
    {
        // İlkin açar = Id.
        b.HasKey(x => x.Id);
        // Hər iki istifadəçi id-si məcburidir (Identity id maks. 450).
        b.Property(x => x.BlockerId).IsRequired().HasMaxLength(450);
        b.Property(x => x.BlockedId).IsRequired().HasMaxLength(450);

        // Hər iki FK AspNetUsers-ə baxır → SQL Server çoxlu kaskad yoluna icazə vermir,
        // ona görə hər ikisi Restrict (istifadəçi silinməsi xüsusi təmizləmə kodu ilə gedir).
        b.HasOne(x => x.Blocker)
            .WithMany()
            .HasForeignKey(x => x.BlockerId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Blocked)
            .WithMany()
            .HasForeignKey(x => x.BlockedId)
            .OnDelete(DeleteBehavior.Restrict);

        // (BlockerId, BlockedId) UNİKAL → eyni cüt üçün təkrar blok sətri yaranmır.
        b.HasIndex(x => new { x.BlockerId, x.BlockedId }).IsUnique();
        // "Məni kim bloklayıb?" yoxlaması üçün indeks (göndərmə qapısında işlənir).
        b.HasIndex(x => x.BlockedId);
    }
}
