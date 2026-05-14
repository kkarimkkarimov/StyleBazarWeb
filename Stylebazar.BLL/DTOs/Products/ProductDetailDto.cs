using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Products;

public class ProductDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; }
    public string? Brand { get; set; }
    public ProductCondition Condition { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime? FeaturedUntil { get; set; }
    public bool IsActive { get; set; }

    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string StoreSlug { get; set; } = string.Empty;

    /// <summary>Seller contact channels — drives the "Satıcıya mesaj yaz" CTA.</summary>
    public string? StorePhone { get; set; }
    public string? StoreWhatsApp { get; set; }
    public string? StoreInstagram { get; set; }
    public string? StoreTelegram { get; set; }

    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;

    public List<ProductImageDto> Images { get; set; } = new();
    public List<ProductSizeDto> Sizes { get; set; } = new();
    public List<ProductColorDto> Colors { get; set; } = new();

    // Rich features (shown in right sidebar)
    public string? Material { get; set; }
    public string? Pattern { get; set; }
    public string? Neckline { get; set; }
    public string? SleeveLength { get; set; }
    public string? Fit { get; set; }
    public string? Season { get; set; }
    public string? Style { get; set; }
    public string? CountryOfOrigin { get; set; }

    public bool HasDelivery { get; set; }

    public int ReviewCount { get; set; }
    public double AverageRating { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
