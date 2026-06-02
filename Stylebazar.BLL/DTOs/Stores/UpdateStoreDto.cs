namespace Stylebazar.BLL.DTOs.Stores;

// UpdateStoreDto — mövcud qeydi YENİLƏMƏK üçün sahələri daşıyan DTO.
public class UpdateStoreDto
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }

    // Contact / social channels
    public string? WhatsAppNumber { get; set; }
    public string? InstagramHandle { get; set; }
    public string? TelegramHandle { get; set; }
    public string? FacebookHandle { get; set; }
    public string? TikTokHandle { get; set; }
}
