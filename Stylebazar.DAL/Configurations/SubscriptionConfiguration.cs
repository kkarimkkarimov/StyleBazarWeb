using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.Notes).HasMaxLength(1000);

        // Restrict (not Cascade) on UserId because (a) SQL Server forbids
        // multiple cascade paths from AspNetUsers to Subscriptions
        // (UserId + ApprovedByAdminId both target the same table) and
        // (b) we want to preserve subscription history as an audit log
        // even if a user is hard-deleted. Soft-delete via DeletedDate is
        // the normal removal path; explicit hard-delete must clear
        // subscriptions first.
        builder.HasOne(s => s.User)
            .WithMany(u => u.Subscriptions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Admin approver — keep history even if the admin account is removed.
        builder.HasOne(s => s.ApprovedByAdmin)
            .WithMany()
            .HasForeignKey(s => s.ApprovedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        // Read patterns:
        //   1. "current active subscription for user X" — every /Seller/* hit
        //   2. "all history for user X newest-first" — admin Detail page
        builder.HasIndex(s => new { s.UserId, s.Status });
        builder.HasIndex(s => new { s.UserId, s.CreatedDate });

        // Filtered unique index: at most ONE Active subscription per user.
        // The global soft-delete query filter also guarantees DeletedDate
        // is NULL on every row reachable through EF, but we still constrain
        // at the DB level because admin SQL or future raw-insert code could
        // bypass the application path.
        builder.HasIndex(s => s.UserId)
            .IsUnique()
            .HasFilter($"[Status] = {(int)SubscriptionStatus.Active} AND [DeletedDate] IS NULL");
    }
}
