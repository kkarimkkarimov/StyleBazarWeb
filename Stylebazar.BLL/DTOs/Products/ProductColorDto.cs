namespace Stylebazar.BLL.DTOs.Products;

public class ProductColorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Hex { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
