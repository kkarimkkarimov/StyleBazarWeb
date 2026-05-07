namespace Stylebazar.BLL.DTOs.Products;

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; }
    public string? Brand { get; set; }
    public int StoreId { get; set; }
    public int CategoryId { get; set; }
    public List<CreateProductSizeDto> Sizes { get; set; } = new();
}
