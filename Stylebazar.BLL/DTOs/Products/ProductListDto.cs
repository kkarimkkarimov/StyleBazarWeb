namespace Stylebazar.BLL.DTOs.Products;

public class ProductListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; }
    public string? Brand { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; }

    public string? MainImageUrl { get; set; }

    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }
}
