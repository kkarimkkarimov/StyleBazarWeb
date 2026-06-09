using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Orders;
using Stylebazar.DAL.Entities.Enums;
using Stylebazar.MVC.Areas.Admin.ViewModels;

namespace Stylebazar.MVC.Areas.Admin.Controllers;

// OrderController (Admin paneli) — bütün sifarişlərin qlobal görünüşü + status idarəetmə.
// Admin istənilən sifarişi görə və statusunu dəyişə bilər (OrderManager admin-i tanıyır).
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrderController : Controller
{
    private readonly IOrderService _orders;

    public OrderController(IOrderService orders)
    {
        _orders = orders;
    }

    // Bütün sifarişlər — opsional status filtri.
    public async Task<IActionResult> Index(OrderStatus? status, CancellationToken ct = default)
    {
        var orders = await _orders.GetAllForAdminAsync(status, ct);
        return View(new AdminOrdersPageViewModel { Orders = orders, StatusFilter = status });
    }

    // Sifariş detalı (admin tam görünüş).
    public async Task<IActionResult> Detail(int id, CancellationToken ct = default)
    {
        var order = await _orders.GetStoreOrderDetailAsync(id, ct);   // admin → bütün sətirlər
        if (order is null) return NotFound();
        return View(new AdminOrderDetailViewModel { Order = order });
    }

    // Status dəyiş (admin override — istənilən keçid).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status, CancellationToken ct = default)
    {
        try
        {
            await _orders.UpdateStatusAsync(id, status, ct);
            TempData["Success"] = "Sifariş statusu yeniləndi.";
        }
        catch (BusinessException ex) { TempData["Error"] = ex.Message; }
        catch (NotFoundException ex) { TempData["Error"] = ex.Message; }
        catch (UnauthorizedException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id });
    }
}
