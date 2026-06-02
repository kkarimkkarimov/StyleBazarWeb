namespace Stylebazar.BLL.DTOs.Products;

// CreateProductSizeDto — yeni qeyd YARATMAQ üçün lazım olan sahələri daşıyan DTO (məlumat ötürmə obyekti).
public class CreateProductSizeDto
{
    public string Size { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
}
