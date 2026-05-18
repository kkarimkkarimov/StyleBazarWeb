using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities;

/// <summary>
/// One row per buyer-initiated contact attempt on a product or store
/// (WhatsApp / Instagram / phone deep-link click). Buyers may be
/// anonymous (UserId null) — IP is anonymized (last octet zeroed)
/// before persistence to stay GDPR-friendly while still allowing
/// "unique visitor" approximation per day.
///
/// Drives Seller Analytics dashboard and is the keystone metric for
/// the premium-subscription monetization model: sellers see ROI,
/// platform sees value-per-listing.
/// </summary>
public class ContactClick : Entity<int>
{
    /// <summary>Optional product (when click happened on a product detail page).</summary>
    public int? ProductId { get; set; }

    /// <summary>Store always set — every contact click resolves to a store owner.</summary>
    public int StoreId { get; set; }

    /// <summary>Authenticated buyer's id; null for anonymous visitors.</summary>
    public string? UserId { get; set; }

    public ContactType ContactType { get; set; }

    /// <summary>Source IP with last octet zeroed (e.g. 192.168.1.0).</summary>
    public string? IpAddress { get; set; }

    /// <summary>Raw User-Agent header — used to bucket device class in analytics.</summary>
    public string? UserAgent { get; set; }

    /// <summary>HTTP Referer header — what page they came from. Optional.</summary>
    public string? Referer { get; set; }

    public virtual Product? Product { get; set; }
    public virtual Store Store { get; set; } = null!;
    public virtual AppUser? User { get; set; }
}
