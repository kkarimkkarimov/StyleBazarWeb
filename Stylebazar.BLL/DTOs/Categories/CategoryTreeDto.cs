namespace Stylebazar.BLL.DTOs.Categories;

// CategoryTreeDto — məlumatı UI / API cavabına ötürən DTO (oxu modeli).
public class CategoryTreeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public List<CategoryTreeDto> Children { get; set; } = new();
}
