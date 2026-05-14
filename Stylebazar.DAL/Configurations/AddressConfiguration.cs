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
        builder.Property(a => a.FullName).IsRequired().HasMaxLength(150);
        builder.Property(a => a.Phone).IsRequired().HasMaxLength(30);
        builder.Property(a => a.City).IsRequired().HasMaxLength(80);
        builder.Property(a => a.AddressLine).IsRequired().HasMaxLength(500);
        builder.Property(a => a.Label).HasMaxLength(40);
        builder.Property(a => a.PostalCode).HasMaxLength(20);
        builder.Property(a => a.Notes).HasMaxLength(500);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.UserId);
    }
}
