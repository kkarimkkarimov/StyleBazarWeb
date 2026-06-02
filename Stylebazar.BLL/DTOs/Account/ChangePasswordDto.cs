namespace Stylebazar.BLL.DTOs.Account;

// ChangePasswordDto — məlumatı UI / API cavabına ötürən DTO (oxu modeli).
public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
