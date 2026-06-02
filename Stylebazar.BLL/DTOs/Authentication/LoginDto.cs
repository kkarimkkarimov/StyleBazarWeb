namespace Stylebazar.BLL.DTOs.Authentication;

// LoginDto — məlumatı UI / API cavabına ötürən DTO (oxu modeli).
public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}
