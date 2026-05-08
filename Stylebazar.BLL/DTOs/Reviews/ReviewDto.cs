namespace Stylebazar.BLL.DTOs.Reviews;

public class ReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string? UserProfileImageUrl { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public List<ReviewImageDto> Images { get; set; } = new();
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
