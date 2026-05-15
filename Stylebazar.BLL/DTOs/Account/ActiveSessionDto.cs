namespace Stylebazar.BLL.DTOs.Account;

public class ActiveSessionDto
{
    public string DeviceLabel { get; set; } = "Windows PC";
    public string? IpAddress { get; set; }
    public DateTime LastSeen { get; set; }
    public bool IsCurrent { get; set; }
}
