using Microsoft.AspNetCore.Mvc;

namespace Stylebazar.MVC.Controllers;

// AboutController — HTTP sorğularını qarşılayan MVC controller-i. İstəyi alır, biznes xidmətini (service) çağırır və nəticəni Razor view / JSON kimi qaytarır.
public class AboutController : Controller
{
    public IActionResult Index() => View();
}
