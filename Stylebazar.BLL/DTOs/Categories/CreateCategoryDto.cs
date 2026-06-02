namespace Stylebazar.BLL.DTOs.Categories;

// CreateCategoryDto — yeni qeyd YARATMAQ üçün lazım olan sahələri daşıyan DTO (məlumat ötürmə obyekti).
public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional. If empty, slug is generated from Name.
    /// </summary>
    public string? Slug { get; set; }

    public int? ParentCategoryId { get; set; }
}
