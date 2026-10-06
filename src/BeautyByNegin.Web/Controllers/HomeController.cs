using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
