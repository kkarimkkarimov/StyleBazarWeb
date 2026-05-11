using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.Property(c => c.FullName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Subject).HasMaxLength(200);
        builder.Property(c => c.Message).IsRequired().HasMaxLength(4000);
        builder.Property(c => c.AdminNote).HasMaxLength(2000);
        builder.Property(c => c.UserIpAddress).HasMaxLength(64);

        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.CreatedDate);
    }
}
