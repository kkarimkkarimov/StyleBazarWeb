namespace Stylebazar.BLL.DTOs.Products;

// ProductImageDto — məlumatı UI / API cavabına ötürən DTO (oxu modeli).
public class ProductImageDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public int DisplayOrder { get; set; }
}
