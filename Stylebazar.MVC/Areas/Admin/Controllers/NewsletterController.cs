using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.Services.Newsletter;
using Stylebazar.MVC.Areas.Admin.ViewModels;

namespace Stylebazar.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class NewsletterController : Controller
{
    private readonly INewsletterService _service;

    public NewsletterController(INewsletterService service) { _service = service; }

    public async Task<IActionResult> Index(bool? isActive, int page = 1, CancellationToken ct = default)
    {
        var result = await _service.GetListAsync(page, 50, isActive, ct);
        return View(new AdminNewsletterPageViewModel
        {
            Subscribers = result, ActiveFilter = isActive, Page = page
        });
    }

    public async Task<IActionResult> ExportCsv(CancellationToken ct)
    {
        var result = await _service.GetListAsync(1, 10_000, true, ct);
        StringBuilder sb = new();
        sb.AppendLine("Email,SubscribedAt,IsActive");
        foreach (var s in result.Items)
            sb.AppendLine($"{s.Email},{s.CreatedDate:O},{s.IsActive}");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "stylebazar-newsletter.csv");
    }
}
