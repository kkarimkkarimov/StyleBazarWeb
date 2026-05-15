using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId).IsRequired();
        builder.Property(a => a.Title).IsRequired().HasMaxLength(60);
        builder.Property(a => a.FullName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Phone).IsRequired().HasMaxLength(30);
        builder.Property(a => a.City).IsRequired().HasMaxLength(80);
        builder.Property(a => a.District).HasMaxLength(80);
        builder.Property(a => a.AddressLine).IsRequired().HasMaxLength(300);
        builder.Property(a => a.PostalCode).HasMaxLength(16);
        builder.Property(a => a.Notes).HasMaxLength(300);

        builder.HasOne(a => a.User)
            .WithMany(u => u.Addresses)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Default address lookups are per-user, ordered "default first then newest"
        builder.HasIndex(a => new { a.UserId, a.IsDefault });
        builder.HasIndex(a => new { a.UserId, a.CreatedDate });
    }
}
