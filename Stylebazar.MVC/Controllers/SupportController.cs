using Microsoft.AspNetCore.Mvc;

namespace Stylebazar.MVC.Controllers;

// SupportController — HTTP sorğularını qarşılayan MVC controller-i. İstəyi alır, biznes xidmətini (service) çağırır və nəticəni Razor view / JSON kimi qaytarır.
public class SupportController : Controller
{
    public IActionResult Index() => View();
}
