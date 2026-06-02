namespace Stylebazar.BLL.DTOs.Products;

// CreateProductColorDto — yeni qeyd YARATMAQ üçün lazım olan sahələri daşıyan DTO (məlumat ötürmə obyekti).
public class CreateProductColorDto
{
    /// <summary>Display name in Az ("Çəhrayı", "Açıq mavi").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Hex with leading #. Drives the swatch fill on the PDP.</summary>
    public string Hex { get; set; } = "#000000";
}
