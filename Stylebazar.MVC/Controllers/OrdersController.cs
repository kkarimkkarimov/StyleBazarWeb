using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.Services.Orders;

namespace Stylebazar.MVC.Controllers;

// OrdersController — alıcının sifariş tarixçəsi və detalı (status izləmə).
[Authorize]
public class OrdersController : Controller
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    // Sifariş tarixçəsi.
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await _orders.GetMyOrdersAsync(ct));

    // Sifariş detalı + status zaman xətti.
    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        var order = await _orders.GetMyOrderDetailAsync(id, ct);
        if (order is null) return NotFound();
        return View(order);
    }
}
