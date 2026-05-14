using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Slug)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.ProfileImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.CoverImageUrl)
            .HasMaxLength(500);

        builder.Property(s => s.ContactPhone)
            .HasMaxLength(20);

        builder.Property(s => s.Address)
            .HasMaxLength(500);

        builder.Property(s => s.WhatsAppNumber).HasMaxLength(30);
        builder.Property(s => s.InstagramHandle).HasMaxLength(60);
        builder.Property(s => s.TelegramHandle).HasMaxLength(60);

        builder.Property(s => s.OwnerId)
            .IsRequired();

        builder.HasOne(s => s.Owner)
            .WithMany(u => u.Stores)
            .HasForeignKey(s => s.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.Slug)
            .IsUnique();

        builder.HasIndex(s => s.OwnerId);
    }
}
