using Stylebazar.BLL.DTOs.Account;

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
