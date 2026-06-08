using Stylebazar.BLL.DTOs.Addresses;
using Stylebazar.BLL.DTOs.Cart;

namespace Stylebazar.MVC.Models;

// CheckoutViewModel — checkout səhifəsinin modeli: səbət xülasəsi + istifadəçinin saxlanmış ünvanları.
public class CheckoutViewModel
{
    public CartSummaryDto Cart { get; set; } = new();
    public List<AddressDto> Addresses { get; set; } = new();
    public int? DefaultAddressId { get; set; }
}
