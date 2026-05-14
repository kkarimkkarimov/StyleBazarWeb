using Stylebazar.BLL.DTOs.Addresses;
using Stylebazar.BLL.DTOs.Cart;

namespace Stylebazar.MVC.ViewModels;

public class CheckoutViewModel
{
    public CartDto Cart { get; set; } = new();
    public List<AddressDto> Addresses { get; set; } = new();
    public int? DefaultAddressId { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Total => Cart.Subtotal + ShippingFee;
}
