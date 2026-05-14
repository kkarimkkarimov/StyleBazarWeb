using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Cart;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Cart;
using Stylebazar.MVC.ViewModels;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.MVC.Controllers;

/// <summary>
/// MVC face of the cart: full /Cart page + JSON AJAX endpoints used by the
/// header cart drawer, the "Add to cart" button on /Shop/Detail, and the
/// qty steppers / remove X on the cart page itself.
/// </summary>
[Authorize]
public class CartController : Controller
{
    private readonly ICartService _cart;

    public CartController(ICartService cart)
    {
        _cart = cart;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        CartDto cart = await _cart.GetMineAsync(ct);
        return View(new CartIndexViewModel { Cart = cart });
    }

    /// <summary>Returns the same CartDto used by /Cart so the drawer can render.</summary>
    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        return Json(await _cart.GetMineAsync(ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        [FromForm] int productId,
        [FromForm] int? productSizeId,
        [FromForm] int quantity,
        CancellationToken ct)
    {
        try
        {
            CartDto cart = await _cart.AddAsync(new AddToCartDto
            {
                ProductId = productId,
                ProductSizeId = productSizeId,
                Quantity = quantity
            }, ct);
            return Json(new { ok = true, cart, count = cart.TotalItemCount });
        }
        catch (ValidationException ex) { return BadRequest(new { ok = false, errors = ex.Errors }); }
        catch (NotFoundException ex)   { return NotFound(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex)   { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        [FromForm] int itemId,
        [FromForm] int quantity,
        CancellationToken ct)
    {
        try
        {
            CartDto cart = await _cart.UpdateQuantityAsync(itemId, new UpdateCartItemDto { Quantity = quantity }, ct);
            return Json(new { ok = true, cart, count = cart.TotalItemCount });
        }
        catch (ValidationException ex) { return BadRequest(new { ok = false, errors = ex.Errors }); }
        catch (NotFoundException ex)   { return NotFound(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex)   { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove([FromForm] int itemId, CancellationToken ct)
    {
        try
        {
            CartDto cart = await _cart.RemoveAsync(itemId, ct);
            return Json(new { ok = true, cart, count = cart.TotalItemCount });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { ok = false, error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        await _cart.ClearAsync(ct);
        return Json(new { ok = true, count = 0 });
    }
}
