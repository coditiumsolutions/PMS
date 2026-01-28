using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Models;

namespace PMS.Web.Controllers;

public class ChallansController : Controller
{
    private readonly PMSDbContext _context;

    public ChallansController(PMSDbContext context)
    {
        _context = context;
    }

    // GET: Challans/Customers
    public async Task<IActionResult> Customers()
    {
        // Keep Payments module sidebar active
        ViewBag.ActiveModule = "Payments";

        var customers = await _context.Customers
            .AsNoTracking()
            .OrderBy(c => c.CustomerNo)
            .ToListAsync();

        return View(customers);
    }
}

