namespace Stylebazar.BLL.DTOs.Account;

/// <summary>
/// Admin-side projection of a user. Includes role + lockout state so the
/// dashboard can render the status badge + role chip without an extra
/// roundtrip per row.
/// </summary>
public class UserAdminDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool EmailConfirmed { get; set; }

    /// <summary>Primary role — first one assigned. Multi-role users are rare here.</summary>
    public string Role { get; set; } = "Buyer";

    /// <summary>True when LockoutEnd is in the future (or MaxValue).</summary>
    public bool IsSuspended { get; set; }
    public DateTime? LockoutEnd { get; set; }

    public int StoreCount { get; set; }
}

public class UserAdminListFilter
{
    public string? Search { get; set; }
    public string? Role { get; set; }           // "Buyer" | "Seller" | "Admin" | null
    public string? Status { get; set; }         // "active" | "suspended" | null
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
