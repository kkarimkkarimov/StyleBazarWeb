using Microsoft.AspNetCore.Mvc;

namespace Stylebazar.MVC.Controllers;

// BlogController — HTTP sorğularını qarşılayan MVC controller-i. İstəyi alır, biznes xidmətini (service) çağırır və nəticəni Razor view / JSON kimi qaytarır.
public class BlogController : Controller
{
    public IActionResult Index(string? cat) => View((object?)cat);
}
