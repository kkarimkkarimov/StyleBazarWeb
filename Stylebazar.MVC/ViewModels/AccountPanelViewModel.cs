using Stylebazar.BLL.DTOs.Account;
using Stylebazar.BLL.DTOs.Addresses;

namespace Stylebazar.MVC.ViewModels;

/// <summary>
/// Shared envelope for every page inside /Account/* (image 1 Dashboard layout).
/// The sidebar partial reads .Profile to render the avatar block + customer code
/// + role-gated links.
/// </summary>
public class AccountPanelViewModel
{
    public AccountProfileDto Profile { get; set; } = new();
}

public class AccountProfilePageViewModel : AccountPanelViewModel
{
    public List<ActiveSessionDto> ActiveSessions { get; set; } = new();
    public List<LoginEventDto> LoginHistory { get; set; } = new();
}

public class AccountAddressesPageViewModel : AccountPanelViewModel
{
    public List<AddressDto> Addresses { get; set; } = new();
}

/// <summary>
/// Notifications timeline (last 30 days). Today only seeded by LoginEvent
/// rows (sign-in, password change, lockout, etc.); future enrichments
/// (new review, new follower) will plug into the same list.
/// </summary>
public class AccountNotificationsPageViewModel : AccountPanelViewModel
{
    public List<LoginEventDto> Events { get; set; } = new();
}
