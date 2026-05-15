using Stylebazar.BLL.DTOs.Account;
using Stylebazar.BLL.DTOs.Addresses;
using Stylebazar.BLL.DTOs.Stores;

namespace Stylebazar.MVC.ViewModels;

/// <summary>
/// Shared envelope for every page inside /Seller/* (image 2 Seller Panel layout).
/// </summary>
public class SellerPanelViewModel
{
    public AccountProfileDto Profile { get; set; } = new();

    /// <summary>The seller's primary store, if any (used for vendor code, social media, etc.).</summary>
    public StoreDetailDto? PrimaryStore { get; set; }
}

public class SellerProfilePageViewModel : SellerPanelViewModel
{
    public List<ActiveSessionDto> ActiveSessions { get; set; } = new();
    public List<LoginEventDto> LoginHistory { get; set; } = new();
    public List<AddressDto> Addresses { get; set; } = new();
}
