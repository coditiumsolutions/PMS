using Microsoft.AspNetCore.Mvc;

namespace PMS.Web.Controllers;

public class CustomersController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ActiveModule = "Customers";
        return View();
    }
}

