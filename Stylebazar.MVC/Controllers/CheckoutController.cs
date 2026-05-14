using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Orders;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Addresses;
using Stylebazar.BLL.Services.Cart;
using Stylebazar.BLL.Services.Orders;
using Stylebazar.DAL.Entities.Enums;
using Stylebazar.MVC.ViewModels;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.MVC.Controllers;

/// <summary>
/// Single /Checkout page: cart summary on the right, 3 collapsible
/// sections on the left (address → payment → review). Place posts to
/// /Checkout/Place which calls IOrderService.PlaceFromCartAsync.
/// </summary>
[Authorize]
public class CheckoutController : Controller
{
    private const decimal FlatShippingFee = 5.00m;

    private readonly ICartService _cart;
    private readonly IAddressService _addresses;
    private readonly IOrderService _orders;

    public CheckoutController(ICartService cart, IAddressService addresses, IOrderService orders)
    {
        _cart = cart;
        _addresses = addresses;
        _orders = orders;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cart = await _cart.GetMineAsync(ct);

        // Don't waste user's time on an empty cart.
        if (cart.Items.Count == 0)
            return RedirectToAction("Index", "Cart");

        var addresses = await _addresses.GetMineAsync(ct);
        int? def = addresses.FirstOrDefault(a => a.IsDefault)?.Id ?? addresses.FirstOrDefault()?.Id;

        return View(new CheckoutViewModel
        {
            Cart = cart,
            Addresses = addresses,
            DefaultAddressId = def,
            ShippingFee = FlatShippingFee
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Place(
        [FromForm] int addressId,
        [FromForm] PaymentMethod paymentMethod,
        [FromForm] string? customerNote,
        CancellationToken ct)
    {
        try
        {
            var order = await _orders.PlaceFromCartAsync(new PlaceOrderDto
            {
                AddressId = addressId,
                PaymentMethod = paymentMethod,
                CustomerNote = customerNote
            }, ct);
            return Json(new { ok = true, orderId = order.Id, orderNumber = order.OrderNumber });
        }
        catch (ValidationException ex) { return BadRequest(new { ok = false, errors = ex.Errors }); }
        catch (NotFoundException ex)   { return NotFound(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex)   { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }
}
