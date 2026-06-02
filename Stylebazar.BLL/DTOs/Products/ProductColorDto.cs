namespace Stylebazar.BLL.DTOs.Products;

// ProductColorDto — məlumatı UI / API cavabına ötürən DTO (oxu modeli).
public class ProductColorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Hex { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
