namespace Stylebazar.BLL.DTOs.Products;

// ProductSizeDto — məlumatı UI / API cavabına ötürən DTO (oxu modeli).
public class ProductSizeDto
{
    public int Id { get; set; }
    public string Size { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
}
