using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Account;

public class LoginEventDto
{
    public int Id { get; set; }
    public LoginEventType EventType { get; set; }
    public string EventLabel { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string DeviceLabel { get; set; } = "Bilinməyən cihaz";
    public DateTime CreatedDate { get; set; }
}
