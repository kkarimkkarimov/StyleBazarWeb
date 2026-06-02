using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Authentication;

// RegisterDto — məlumatı UI / API cavabına ötürən DTO (oxu modeli).
public class RegisterDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}
