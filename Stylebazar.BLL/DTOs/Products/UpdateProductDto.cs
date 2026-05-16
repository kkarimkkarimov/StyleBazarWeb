using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Products;

public class UpdateProductDto
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
    public int CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool HasDelivery { get; set; }

    // ---- Variant collections — replace-all semantics ----
    // Sending these REPLACES the product's current sizes/colors with the new
    // set. The manager diffs against existing rows to keep stable Ids where
    // labels match, avoiding orphan rows.
    public List<CreateProductSizeDto> Sizes { get; set; } = new();
    public List<CreateProductColorDto> Colors { get; set; } = new();

    // ---- 8 free-text feature fields ----
    public string? Material { get; set; }
    public string? Pattern { get; set; }
    public string? Neckline { get; set; }
    public string? SleeveLength { get; set; }
    public string? Fit { get; set; }
    public string? Season { get; set; }
    public string? Style { get; set; }
    public string? CountryOfOrigin { get; set; }
}
