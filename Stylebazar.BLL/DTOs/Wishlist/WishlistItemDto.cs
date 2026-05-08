namespace Stylebazar.BLL.DTOs.Wishlist;

public class WishlistItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string? MainImageUrl { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}
