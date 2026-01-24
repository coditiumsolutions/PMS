using Microsoft.AspNetCore.Mvc;

namespace PMS.Web.Controllers;

public class TransfersController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ActiveModule = "Transfers";
        return View();
    }

    public IActionResult Dashboard()
    {
        ViewBag.ActiveModule = "Transfers";
        return View();
    }

    public IActionResult Details()
    {
        ViewBag.ActiveModule = "Transfers";
        return View();
    }

    public IActionResult Operations()
    {
        ViewBag.ActiveModule = "Transfers";
        return View();
    }

    public IActionResult Summary()
    {
        ViewBag.ActiveModule = "Transfers";
        return View();
    }

    public IActionResult History()
    {
        ViewBag.ActiveModule = "Transfers";
        return View();
    }
}

