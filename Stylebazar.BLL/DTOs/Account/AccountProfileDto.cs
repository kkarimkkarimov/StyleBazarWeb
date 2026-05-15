namespace Stylebazar.BLL.DTOs.Account;

/// <summary>
/// Read DTO returned by GET /Account/me — populates the Profile page form
/// and the sidebar avatar block on both the Dashboard and Seller Panel.
/// </summary>
public class AccountProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedDate { get; set; }

    /// <summary>"#usr_XXXXXXXX" short id — stable, derived from Id.</summary>
    public string CustomerCode { get; set; } = string.Empty;

    /// <summary>"#vnd_XXXXXXXX" short id when the user owns any store, else null.</summary>
    public string? VendorCode { get; set; }

    public List<string> Roles { get; set; } = new();
    public bool IsEmailConfirmed { get; set; }
}
