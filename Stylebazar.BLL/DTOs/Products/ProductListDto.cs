using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Products;

public class ProductListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Short product description shown as a 2-line preview under the name
    /// in rich product cards. Mapped from Product.Description.
    /// </summary>
    public string? Description { get; set; }

    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; }
    public string? Brand { get; set; }
    public ProductCondition Condition { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; }

    public string? MainImageUrl { get; set; }

    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Average review rating (0 when no reviews). Drives the star row on rich cards.</summary>
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }

    public DateTime CreatedDate { get; set; }
}
