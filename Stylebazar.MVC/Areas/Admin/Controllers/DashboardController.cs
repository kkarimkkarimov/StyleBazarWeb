using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Account;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.DAL.Entities.Enums;
using Stylebazar.BLL.Services.Account;
using Stylebazar.BLL.Services.Contact;
using Stylebazar.BLL.Services.LoginEvents;
using Stylebazar.BLL.Services.Products;
using Stylebazar.BLL.Services.Stores;
using Stylebazar.BLL.Services.Stories;
using Stylebazar.MVC.Areas.Admin.ViewModels;

namespace Stylebazar.MVC.Areas.Admin.Controllers;

// DashboardController (Admin paneli) — yalnız adminə açıq əməliyyatlar üçün controller. İstəyi alır, biznes xidmətini çağırır, idarəetmə nəticəsini/səhifəsini qaytarır.
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly IAccountService _account;
    private readonly IStoreService _stores;
    private readonly IProductService _products;
    private readonly IStoryService _stories;
    private readonly IContactMessageService _contact;
    private readonly ILoginEventService _events;

    public DashboardController(
        IAccountService account,
        IStoreService stores,
        IProductService products,
        IStoryService stories,
        IContactMessageService contact,
        ILoginEventService events)
    {
        _account = account;
        _stores = stores;
        _products = products;
        _stories = stories;
        _contact = contact;
        _events = events;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var usersAll = await _account.GetUsersAsync(new UserAdminListFilter { Page = 1, PageSize = 1 }, ct);
        var sellersOnly = await _account.GetUsersAsync(new UserAdminListFilter { Role = "Seller", Page = 1, PageSize = 1 }, ct);
        var allStores = await _stores.GetListAsync(new Stylebazar.BLL.DTOs.Stores.StoreListQueryDto { Page = 1, PageSize = 1 }, ct);
        var allProducts = await _products.GetListAsync(new ProductListQueryDto { Page = 1, PageSize = 1 }, ct);
        var allStories = await _stories.GetAllForAdminAsync(take: 500, ct);
        var unreadContact = await _contact.GetListAsync(1, 1, ContactStatus.New, ct);

        var recentUsers = await _account.GetUsersAsync(new UserAdminListFilter { Page = 1, PageSize = 8 }, ct);
        var recentMsgs = await _contact.GetListAsync(1, 8, null, ct);
        // Platform-wide last 10 auth events (sign-in, sign-up, locked, password
        // changed, etc.) — gives the admin a passive security feed without
        // opening an individual user's detail page.
        var recentLogins = await _events.GetRecentForAdminAsync(10, ct);

        return View(new AdminDashboardViewModel
        {
            TotalUsers = usersAll.Count,
            TotalSellers = sellersOnly.Count,
            TotalStores = allStores.Count,
            TotalProducts = allProducts.Count,
            ActiveStories = allStories.Count(s => s.Story.ExpiresAt > DateTime.UtcNow),
            UnreadContactMessages = unreadContact.Count,
            RecentUsers = recentUsers.Items.ToList(),
            RecentMessages = recentMsgs.Items.ToList(),
            RecentLoginEvents = recentLogins
        });
    }
}
