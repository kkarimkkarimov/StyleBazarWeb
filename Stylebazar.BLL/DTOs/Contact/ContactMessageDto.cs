using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Contact;

// ContactMessageDto — məlumatı UI / API cavabına ötürən DTO (oxu modeli).
public class ContactMessageDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
    public ContactStatus Status { get; set; }
    public string? AdminNote { get; set; }
    public DateTime CreatedDate { get; set; }
}
