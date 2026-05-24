using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Account;

/// <summary>
/// Admin-dashboard projection — same shape as <see cref="LoginEventDto"/> plus
/// the owning user (id + display name + email) so the table can link straight
/// to that user's detail page.
/// </summary>
public class AdminLoginEventDto
{
    public int Id { get; set; }
    public LoginEventType EventType { get; set; }
    public string EventLabel { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string DeviceLabel { get; set; } = "Bilinməyən cihaz";
    public DateTime CreatedDate { get; set; }

    public string UserId { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
}
