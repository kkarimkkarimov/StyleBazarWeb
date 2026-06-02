namespace Stylebazar.BLL.DTOs.Categories;

// CategoryDetailDto — məlumatı UI / API cavabına ötürən DTO (oxu modeli).
public class CategoryDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public List<CategoryListDto> ChildCategories { get; set; } = new();
}
