namespace Stylebazar.BLL.DTOs.Newsletter;

// NewsletterSubscriberDto — məlumatı UI / API cavabına ötürən DTO (oxu modeli).
public class NewsletterSubscriberDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UnsubscribedAt { get; set; }
}
