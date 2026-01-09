using Microsoft.AspNetCore.Mvc;

namespace PMS.Web.Controllers;

public class SetupController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ActiveModule = "Setup";
        return View();
    }
}

