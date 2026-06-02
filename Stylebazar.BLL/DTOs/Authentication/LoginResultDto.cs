namespace Stylebazar.BLL.DTOs.Authentication;

// LoginResultDto — məlumatı UI / API cavabına ötürən DTO (oxu modeli).
public class LoginResultDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
