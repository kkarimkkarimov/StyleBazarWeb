using Microsoft.EntityFrameworkCore;                    // Fluent API genişləndirmələri.
using Microsoft.EntityFrameworkCore.Metadata.Builders;  // EntityTypeBuilder<T> tipi.
using Stylebazar.DAL.Entities;                          // NewsletterSubscriber entity-si.

// EF konfiqurasiyalarının ad fəzası.
namespace Stylebazar.DAL.Configurations;

// NewsletterSubscriber entity-sinin bazaya map qaydaları (Fluent API).
public class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> builder)
    {
        // Email və çıxış-tokeni məcburidir.
        builder.Property(n => n.Email).IsRequired().HasMaxLength(256);
        builder.Property(n => n.UnsubscribeToken).IsRequired().HasMaxLength(64);

        // Unikal indekslər: bir email yalnız bir dəfə abunə ola bilər; token da təkdir (təhlükəsiz çıxış).
        builder.HasIndex(n => n.Email).IsUnique();
        builder.HasIndex(n => n.UnsubscribeToken).IsUnique();
    }
}
