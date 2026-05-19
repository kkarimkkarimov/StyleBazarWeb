using Microsoft.AspNetCore.Identity;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities
{
    public class AppUser : IdentityUser<string>
    {
        public required string FullName { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? Bio { get; set; }
        public DateTime CreatedDate { get; set; }

        // ------ Denormalized subscription state (hybrid model) ------
        // The Subscription table holds the immutable change log; these
        // fields cache the *current* plan for fast read paths (every
        // /Seller/* request, every catalog ORDER BY, every Store DTO).
        // Always updated atomically with the matching Subscription row.

        /// <summary>Active plan tier. Defaults to Free for every new account.</summary>
        public SubscriptionPlan CurrentPlan { get; set; } = SubscriptionPlan.Free;

        /// <summary>UTC. Null = no expiry (lifetime / Free). When &lt; UtcNow → lazy-downgrade fires.</summary>
        public DateTime? PlanExpiresAt { get; set; }

        public virtual ICollection<Store> Stores { get; set; } = new List<Store>();
        public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
        public virtual ICollection<StoreFollower> StoreFollows { get; set; } = new List<StoreFollower>();
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}
