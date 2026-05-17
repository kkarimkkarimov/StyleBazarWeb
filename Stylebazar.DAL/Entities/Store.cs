using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

public class Store : Entity<int>
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public required string ProfileImageUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public required string OwnerId { get; set; }
    public bool IsActive { get; set; }

    // --- Contact channels (the heart of our marketplace UX) ---

    /// <summary>
    /// E.164 phone number used for wa.me deep links (e.g. "994508204111").
    /// We store digits-only; the UI prepends https://wa.me/ on click.
    /// </summary>
    public string? WhatsAppNumber { get; set; }

    /// <summary>
    /// Instagram username without the @ ("useremda" → instagram.com/useremda).
    /// </summary>
    public string? InstagramHandle { get; set; }

    /// <summary>
    /// Telegram username without the @ ("useremda" → t.me/useremda).
    /// </summary>
    public string? TelegramHandle { get; set; }

    /// <summary>Facebook page/profile slug ("yourpage" → facebook.com/yourpage).</summary>
    public string? FacebookHandle { get; set; }

    /// <summary>TikTok username without the @ ("useremda" → tiktok.com/@useremda).</summary>
    public string? TikTokHandle { get; set; }

    public virtual AppUser Owner { get; set; } = null!;
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public virtual ICollection<StoreFollower> Followers { get; set; } = new List<StoreFollower>();
}
