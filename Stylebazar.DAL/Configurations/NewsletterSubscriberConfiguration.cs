using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Configurations;

public class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> builder)
    {
        builder.Property(n => n.Email).IsRequired().HasMaxLength(256);
        builder.Property(n => n.UnsubscribeToken).IsRequired().HasMaxLength(64);

        builder.HasIndex(n => n.Email).IsUnique();
        builder.HasIndex(n => n.UnsubscribeToken).IsUnique();
    }
}
