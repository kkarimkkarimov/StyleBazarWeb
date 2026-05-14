using Stylebazar.BLL.DTOs.Addresses;

namespace Stylebazar.MVC.ViewModels;

public class AddressesIndexViewModel
{
    public List<AddressDto> Items { get; set; } = new();
    public bool HasItems => Items.Count > 0;
}
