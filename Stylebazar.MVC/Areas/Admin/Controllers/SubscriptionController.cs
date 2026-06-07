using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.Services.Subscriptions;
using Stylebazar.DAL.Entities.Enums;
using Stylebazar.MVC.Areas.Admin.ViewModels;

namespace Stylebazar.MVC.Areas.Admin.Controllers;

// SubscriptionController (Admin paneli) — bütün abunəlik/ödəniş qeydlərinin qlobal görünüşü.
// Hər sətir istifadəçinin Detail səhifəsinə keçid verir (plan idarəetməsi orada — ChangeSubscription).
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SubscriptionController : Controller
{
    private readonly ISubscriptionService _subscriptions;

    public SubscriptionController(ISubscriptionService subscriptions)
    {
        _subscriptions = subscriptions;
    }

    public async Task<IActionResult> Index(
        SubscriptionPlan? plan, SubscriptionStatus? status, int page = 1, CancellationToken ct = default)
    {
        var items = await _subscriptions.GetAllForAdminAsync(plan, status, page, 20, ct);
        return View(new AdminSubscriptionsPageViewModel
        {
            Items = items,
            PlanFilter = plan,
            StatusFilter = status
        });
    }
}
