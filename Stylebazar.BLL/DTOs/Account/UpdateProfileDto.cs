namespace Stylebazar.BLL.DTOs.Account;

// UpdateProfileDto — mövcud qeydi YENİLƏMƏK üçün sahələri daşıyan DTO.
public class UpdateProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Bio { get; set; }
}
