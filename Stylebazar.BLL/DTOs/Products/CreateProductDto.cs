using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Products;

public class CreateProductDto
{
    // ---- Core ----
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; }
    public string? Brand { get; set; }
    public ProductCondition Condition { get; set; } = ProductCondition.New;
    public int StoreId { get; set; }
    public int CategoryId { get; set; }

    /// <summary>True if the seller delivers this item (green badge on the PDP).</summary>
    public bool HasDelivery { get; set; }

    // ---- Variant collections ----
    public List<CreateProductSizeDto> Sizes { get; set; } = new();
    public List<CreateProductColorDto> Colors { get; set; } = new();

    // ---- 8 free-text feature fields (all optional) ----
    public string? Material { get; set; }
    public string? Pattern { get; set; }
    public string? Neckline { get; set; }
    public string? SleeveLength { get; set; }
    public string? Fit { get; set; }
    public string? Season { get; set; }
    public string? Style { get; set; }
    public string? CountryOfOrigin { get; set; }
}
