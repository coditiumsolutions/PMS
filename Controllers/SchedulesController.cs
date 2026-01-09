using Microsoft.AspNetCore.Mvc;

namespace PMS.Web.Controllers;

public class SchedulesController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ActiveModule = "Schedules";
        return View();
    }
}

