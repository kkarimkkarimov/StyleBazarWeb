namespace Stylebazar.BLL.DTOs.Stores;

// StoreDetailDto — məlumatı UI / API cavabına ötürən DTO (oxu modeli).
public class StoreDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ProfileImageUrl { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }

    // Contact / social channels
    public string? WhatsAppNumber { get; set; }
    public string? InstagramHandle { get; set; }
    public string? TelegramHandle { get; set; }
    public string? FacebookHandle { get; set; }
    public string? TikTokHandle { get; set; }

    public string OwnerId { get; set; } = string.Empty;
    public string OwnerFullName { get; set; } = string.Empty;

    /// <summary>True when owner.CurrentPlan == VIP — drives the ✓ badge next to the store name.</summary>
    public bool IsVerified { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
