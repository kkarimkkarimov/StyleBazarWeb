using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

public class ContactClickConfiguration : IEntityTypeConfiguration<ContactClick>
{
    public void Configure(EntityTypeBuilder<ContactClick> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.IpAddress).HasMaxLength(64);
        builder.Property(c => c.UserAgent).HasMaxLength(500);
        builder.Property(c => c.Referer).HasMaxLength(500);

        builder.HasOne(c => c.Store)
            .WithMany()
            .HasForeignKey(c => c.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Product)
            .WithMany()
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes serve the seller analytics dashboard:
        //   1. By store, ordered by date — "clicks per day for my store"
        //   2. By product, ordered by date — "top products by clicks"
        //   3. By contact type — "WhatsApp vs phone breakdown"
        builder.HasIndex(c => new { c.StoreId, c.CreatedDate });
        builder.HasIndex(c => new { c.ProductId, c.CreatedDate });
        builder.HasIndex(c => c.ContactType);
    }
}
