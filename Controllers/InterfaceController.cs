using Microsoft.AspNetCore.Mvc;

namespace PMS.Web.Controllers;

public class InterfaceController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

