using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Orders;
using Stylebazar.MVC.ViewModels;

namespace Stylebazar.MVC.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly IOrderService _orders;

    public OrderController(IOrderService orders)
    {
        _orders = orders;
    }

    /// <summary>Order placed confirmation screen.</summary>
    [HttpGet]
    public async Task<IActionResult> Success(int id, CancellationToken ct)
    {
        try
        {
            var order = await _orders.GetByIdAsync(id, ct);
            return View(new OrderDetailViewModel { Order = order });
        }
        catch (NotFoundException) { return NotFound(); }
        catch (BusinessException) { return Forbid(); }
    }

    /// <summary>List of all of the current user's orders.</summary>
    [HttpGet("Account/Orders")]
    public async Task<IActionResult> History(int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var orders = await _orders.GetMineAsync(page, pageSize, ct);
        return View("History", new OrderHistoryViewModel { Orders = orders, Page = page, PageSize = pageSize });
    }

    /// <summary>Full detail of a single order.</summary>
    [HttpGet("Account/Orders/{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        try
        {
            var order = await _orders.GetByIdAsync(id, ct);
            return View("Detail", new OrderDetailViewModel { Order = order });
        }
        catch (NotFoundException) { return NotFound(); }
        catch (BusinessException) { return Forbid(); }
    }

    /// <summary>Buyer cancels own order (pre-shipping only).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel([FromForm] int id, CancellationToken ct)
    {
        try
        {
            var order = await _orders.CancelMineAsync(id, ct);
            return Json(new { ok = true, status = order.Status.ToString() });
        }
        catch (NotFoundException ex) { return NotFound(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex) { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }
}
