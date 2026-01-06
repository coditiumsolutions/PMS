using Microsoft.AspNetCore.Mvc;

namespace PMS.Web.Controllers;

public class PlotsController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ActiveModule = "Plots";
        return View();
    }
}

