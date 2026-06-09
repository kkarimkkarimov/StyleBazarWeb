using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Stylebazar.BLL.DTOs.Cart;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Cart;

namespace Stylebazar.MVC.Controllers;

// CartController — saytdaxili səbət. [Authorize] — səbət giriş tələb edir.
[Authorize]
public class CartController : Controller
{
    private readonly ICartService _cart;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartService cart, ILogger<CartController> logger)
    {
        _cart = cart;
        _logger = logger;
    }

    // Səbət səhifəsi.
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await _cart.GetMyCartAsync(ct));

    // "Səbətə at" — AJAX (məhsul detalı/kartından). JSON {ok, count} qaytarır ki, rozetka yenilənsin.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> Add(AddToCartDto dto, CancellationToken ct)
    {
        try
        {
            await _cart.AddAsync(dto, ct);
            int count = await _cart.GetMyCountAsync(ct);
            return Json(new { ok = true, count, message = "Səbətə əlavə olundu" });
        }
        catch (BusinessException ex) { return Json(new { ok = false, error = ex.Message }); }
        catch (NotFoundException ex) { return Json(new { ok = false, error = ex.Message }); }
        catch (UnauthorizedException ex) { return Json(new { ok = false, error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Səbətə əlavə alınmadı (product {ProductId})", dto.ProductId);
            return Json(new { ok = false, error = "Səbətə əlavə olunmadı. Yenidən cəhd et." });
        }
    }

    // Səbət səhifəsində sayı dəyiş. AJAX-dırsa JSON (canlı yenilənmə), əks halda form post → Index.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(int id, int quantity, CancellationToken ct)
    {
        try { await _cart.UpdateQuantityAsync(id, quantity, ct); }
        catch (NotFoundException) { /* artıq yoxdursa səssizcə keç */ }
        if (IsAjax()) return await CartStateJsonAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }

    // Səbətdən sil. AJAX-dırsa JSON, əks halda form post → Index.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id, CancellationToken ct)
    {
        await _cart.RemoveAsync(id, ct);
        if (IsAjax()) return await CartStateJsonAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }

    // AJAX səbət əməliyyatlarından sonra cari vəziyyəti JSON kimi qaytarır (sətir cəmi, ara cəm, say).
    private async Task<IActionResult> CartStateJsonAsync(int itemId, CancellationToken ct)
    {
        CartSummaryDto cart = await _cart.GetMyCartAsync(ct);
        CartItemDto? line = cart.Items.FirstOrDefault(i => i.Id == itemId);
        return Json(new
        {
            ok = true,
            removed = line is null,
            quantity = line?.Quantity ?? 0,
            lineTotalText = (line?.LineTotal ?? 0m).ToString("0.##", CultureInfo.InvariantCulture),
            subtotalText = cart.Subtotal.ToString("0.##", CultureInfo.InvariantCulture),
            totalQuantity = cart.TotalQuantity,
            isEmpty = cart.IsEmpty
        });
    }

    private bool IsAjax() =>
        string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
}
