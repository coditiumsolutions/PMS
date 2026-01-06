using Microsoft.AspNetCore.Mvc;

namespace PMS.Web.Controllers;

public class PaymentsController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ActiveModule = "Payments";
        return View();
    }
}

