using Microsoft.AspNetCore.Mvc;

namespace PMS.Web.Controllers;

public class SchedulesController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ActiveModule = "Schedules";
        return View();
    }

    public IActionResult Dashboard()
    {
        ViewBag.ActiveModule = "Schedules";
        return View();
    }

    public IActionResult Details()
    {
        ViewBag.ActiveModule = "Schedules";
        return View();
    }

    public IActionResult Operations()
    {
        ViewBag.ActiveModule = "Schedules";
        return View();
    }

    public IActionResult Summary()
    {
        ViewBag.ActiveModule = "Schedules";
        return View();
    }

    public IActionResult History()
    {
        ViewBag.ActiveModule = "Schedules";
        return View();
    }
}

