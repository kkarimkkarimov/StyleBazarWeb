namespace Stylebazar.BLL.DTOs.Categories;

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional. If empty, slug is generated from Name.
    /// </summary>
    public string? Slug { get; set; }

    public int? ParentCategoryId { get; set; }
}
