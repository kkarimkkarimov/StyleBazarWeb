using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Stylebazar.BLL.DTOs.Orders;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Addresses;
using Stylebazar.BLL.Services.Cart;
using Stylebazar.BLL.Services.Orders;
using Stylebazar.BLL.Services.Payments;
using Stylebazar.DAL.Entities;
using Stylebazar.MVC.Models;

namespace Stylebazar.MVC.Controllers;

// CheckoutController — səbət → çatdırılma ünvanı → Stripe ödənişi → sifariş.
// Axın: Index (ünvan seç/yaz) → Place (sifariş yarat + Stripe sessiya, AJAX→URL) → Stripe →
// Success (ödənişi təsdiqlə, sifarişi Paid et) və ya Cancel.
[Authorize]
public class CheckoutController : Controller
{
    private readonly ICartService _cart;
    private readonly IOrderService _orders;
    private readonly IAddressService _addresses;
    private readonly IPaymentService _payments;
    private readonly UserManager<AppUser> _users;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        ICartService cart,
        IOrderService orders,
        IAddressService addresses,
        IPaymentService payments,
        UserManager<AppUser> users,
        ILogger<CheckoutController> logger)
    {
        _cart = cart;
        _orders = orders;
        _addresses = addresses;
        _payments = payments;
        _users = users;
        _logger = logger;
    }

    // Checkout səhifəsi: səbət boşdursa səbətə qaytar; əks halda ünvan seçimi + xülasə göstər.
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cart = await _cart.GetMyCartAsync(ct);
        if (cart.IsEmpty)
        {
            TempData["Error"] = "Səbətiniz boşdur.";
            return RedirectToAction("Index", "Cart");
        }

        var addresses = await _addresses.GetMineAsync(ct);
        var vm = new CheckoutViewModel
        {
            Cart = cart,
            Addresses = addresses,
            DefaultAddressId = addresses.FirstOrDefault(a => a.IsDefault)?.Id ?? addresses.FirstOrDefault()?.Id
        };
        return View(vm);
    }

    // Sifarişi yarat + Stripe Checkout sessiyası qaytar (AJAX → window.location).
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> Place(PlaceOrderDto dto, CancellationToken ct)
    {
        try
        {
            // 1) Səbətdən "Pending" sifariş yarat (çatdırılma snapshot-u ilə).
            int orderId = await _orders.CreateFromCartAsync(dto, ct);

            // 2) Sifarişin sətirlərini Stripe line item-lərinə çevir.
            OrderDetailDto? order = await _orders.GetMyOrderDetailAsync(orderId, ct);
            if (order is null)
                return Json(new { ok = false, error = "Sifariş yaradıla bilmədi." });

            var lines = order.Items.Select(i => new OrderCheckoutLine(
                BuildLineName(i.ProductName, i.Size, i.Color), i.UnitPrice, i.Quantity)).ToList();

            AppUser? user = await _users.GetUserAsync(User);

            string successUrl = Url.Action(nameof(Success), "Checkout", null, Request.Scheme)!
                                + "?session_id={CHECKOUT_SESSION_ID}";
            string cancelUrl = Url.Action(nameof(Cancel), "Checkout", new { orderId }, Request.Scheme)!;

            // 3) Stripe Checkout sessiyası → URL.
            string url = await _payments.CreateOrderCheckoutUrlAsync(
                new OrderCheckoutRequest(orderId, user?.Email, user?.FullName, lines),
                successUrl, cancelUrl, ct);

            return Json(new { ok = true, url });
        }
        catch (BusinessException ex) { return Json(new { ok = false, error = ex.Message }); }
        catch (NotFoundException ex) { return Json(new { ok = false, error = ex.Message }); }
        catch (UnauthorizedException ex) { return Json(new { ok = false, error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Checkout/Place alınmadı");
            return Json(new { ok = false, error = "Ödəniş başlana bilmədi. Yenidən cəhd et." });
        }
    }

    // Stripe uğurlu ödənişdən sonra bura qaytarır → sifarişi Paid et (idempotent) → təsdiq səhifəsi.
    [HttpGet]
    public async Task<IActionResult> Success(string? session_id, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(session_id))
        {
            try
            {
                PaidOrderCheckout? paid = await _payments.GetCompletedOrderCheckoutAsync(session_id, ct);
                if (paid is not null)
                {
                    await _orders.MarkPaidAsync(paid.OrderId, paid.PaymentReference, ct);
                    TempData["Success"] = "Ödəniş uğurlu oldu! Sifarişin qəbul edildi. 🎉";
                    return RedirectToAction("Detail", "Orders", new { id = paid.OrderId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Checkout/Success — sifariş təsdiqi alınmadı (session {Session})", session_id);
            }
        }

        TempData["Success"] = "Ödəniş alındı. Sifarişin bir neçə saniyəyə təsdiqlənəcək.";
        return RedirectToAction("Index", "Orders");
    }

    // İstifadəçi Stripe-da ödənişi ləğv etdi → səbətə qayıt (sifariş Pending qalır).
    [HttpGet]
    public IActionResult Cancel(int? orderId)
    {
        TempData["Error"] = "Ödəniş ləğv edildi. Sifarişin tamamlanmadı.";
        return RedirectToAction("Index", "Cart");
    }

    // Line adı: "Köynək (Ölçü: M, Rəng: Qara)"
    private static string BuildLineName(string name, string? size, string? color)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(size)) parts.Add($"Ölçü: {size}");
        if (!string.IsNullOrWhiteSpace(color)) parts.Add($"Rəng: {color}");
        return parts.Count > 0 ? $"{name} ({string.Join(", ", parts)})" : name;
    }
}
