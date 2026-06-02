namespace Stylebazar.BLL.DTOs.Account;

// ActiveSessionDto — məlumatı UI / API cavabına ötürən DTO (oxu modeli).
public class ActiveSessionDto
{
    public string DeviceLabel { get; set; } = "Windows PC";
    public string? IpAddress { get; set; }
    public DateTime LastSeen { get; set; }
    public bool IsCurrent { get; set; }
}
