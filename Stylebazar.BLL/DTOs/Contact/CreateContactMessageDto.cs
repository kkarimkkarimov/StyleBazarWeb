namespace Stylebazar.BLL.DTOs.Contact;

// CreateContactMessageDto — yeni qeyd YARATMAQ üçün lazım olan sahələri daşıyan DTO (məlumat ötürmə obyekti).
public class CreateContactMessageDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
}
