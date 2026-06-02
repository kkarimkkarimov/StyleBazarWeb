namespace Stylebazar.BLL.DTOs.Reviews;

// CreateReviewDto — yeni qeyd YARATMAQ üçün lazım olan sahələri daşıyan DTO (məlumat ötürmə obyekti).
public class CreateReviewDto
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
