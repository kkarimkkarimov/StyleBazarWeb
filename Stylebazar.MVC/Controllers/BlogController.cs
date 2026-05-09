using Microsoft.AspNetCore.Mvc;

namespace Stylebazar.MVC.Controllers;

public class BlogController : Controller
{
    public IActionResult Index(string? cat) => View((object?)cat);
}
