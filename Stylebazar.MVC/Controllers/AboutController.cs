using Microsoft.AspNetCore.Mvc;

namespace Stylebazar.MVC.Controllers;

public class AboutController : Controller
{
    public IActionResult Index() => View();
}
