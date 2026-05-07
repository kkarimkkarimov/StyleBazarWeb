namespace Stylebazar.BLL.DTOs.Stores;

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

    public string OwnerId { get; set; } = string.Empty;
    public string OwnerFullName { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
