using Microsoft.AspNetCore.Mvc;

namespace PMS.Web.Controllers;

public class TransfersController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ActiveModule = "Transfers";
        return View();
    }
}

