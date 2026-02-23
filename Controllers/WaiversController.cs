using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Models;

namespace PMS.Web.Controllers;

[Route("Waivers")]
public class WaiversController : Controller
{
    private readonly PMSDbContext _context;

    public WaiversController(PMSDbContext context)
    {
        _context = context;
    }

    // GET: Waivers/LookupCustomer?customerNo=CUST001
    [HttpGet("LookupCustomer")]
    public async Task<IActionResult> LookupCustomer(string customerNo)
    {
        if (string.IsNullOrWhiteSpace(customerNo))
        {
            return BadRequest(new { success = false, message = "CustomerNo is required." });
        }

        var normalizedCustomerNo = customerNo.Trim();

        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerNo == normalizedCustomerNo);

        if (customer == null)
        {
            return NotFound(new { success = false, message = "Customer not found." });
        }

        var inventory = await _context.InventoryDetails
            .AsNoTracking()
            .Where(i => i.CustomerNo == normalizedCustomerNo)
            .OrderByDescending(i => i.UID)
            .FirstOrDefaultAsync();

        var propertyDetail = inventory == null
            ? string.Empty
            : $"{inventory.Block ?? string.Empty} {inventory.PlotNo ?? inventory.UnitNo ?? string.Empty}".Trim();

        return Json(new
        {
            success = true,
            data = new
            {
                customerNo = customer.CustomerNo ?? normalizedCustomerNo,
                customerName = customer.FullName,
                projectName = inventory?.Project ?? string.Empty,
                propertyDetail,
                requestedPlotType = inventory?.UnitSize ?? string.Empty
            }
        });
    }

    // GET: Waivers
    [HttpGet]
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveModule = "Waivers";
        return View(await _context.Waivers.OrderByDescending(w => w.Uid).ToListAsync());
    }

    // GET: Waivers/Details/5
    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int? id)
    {
        ViewBag.ActiveModule = "Waivers";
        if (id == null)
        {
            return NotFound();
        }

        var waiver = await _context.Waivers.FirstOrDefaultAsync(m => m.Uid == id);
        if (waiver == null)
        {
            return NotFound();
        }

        return View(waiver);
    }

    // GET: Waivers/Create
    [HttpGet("Create")]
    public IActionResult Create()
    {
        ViewBag.ActiveModule = "Waivers";
        return View(new Waiver { CreatedDate = DateTime.Now, IsActive = true });
    }

    // POST: Waivers/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Uid,CustomerNo,ProjectName,CustomerName,WaiveOffType,PaymentDesc,InstallmentNo,DueAmount,PropertyDetail,RequestedPlotType,WaiveOffAmount,CreatedDate,CreatedBy,IsActive")] Waiver waiver)
    {
        ViewBag.ActiveModule = "Waivers";
        NormalizeNonNullableDbFields(waiver);
        if (ModelState.IsValid)
        {
            if (!waiver.CreatedDate.HasValue)
            {
                waiver.CreatedDate = DateTime.Now;
            }
            if (!waiver.IsActive.HasValue)
            {
                waiver.IsActive = true;
            }

            _context.Add(waiver);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(waiver);
    }

    // GET: Waivers/Edit/5
    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int? id)
    {
        ViewBag.ActiveModule = "Waivers";
        if (id == null)
        {
            return NotFound();
        }

        var waiver = await _context.Waivers.FindAsync(id);
        if (waiver == null)
        {
            return NotFound();
        }
        return View(waiver);
    }

    // POST: Waivers/Edit/5
    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Uid,CustomerNo,ProjectName,CustomerName,WaiveOffType,PaymentDesc,InstallmentNo,DueAmount,PropertyDetail,RequestedPlotType,WaiveOffAmount,CreatedDate,CreatedBy,IsActive")] Waiver waiver)
    {
        ViewBag.ActiveModule = "Waivers";
        NormalizeNonNullableDbFields(waiver);
        if (id != waiver.Uid)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(waiver);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WaiverExists(waiver.Uid))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(waiver);
    }

    // GET: Waivers/Delete/5
    [HttpGet("Delete/{id}")]
    public async Task<IActionResult> Delete(int? id)
    {
        ViewBag.ActiveModule = "Waivers";
        if (id == null)
        {
            return NotFound();
        }

        var waiver = await _context.Waivers.FirstOrDefaultAsync(m => m.Uid == id);
        if (waiver == null)
        {
            return NotFound();
        }

        return View(waiver);
    }

    // POST: Waivers/Delete/5
    [HttpPost("Delete/{id}"), ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        ViewBag.ActiveModule = "Waivers";
        var waiver = await _context.Waivers.FindAsync(id);
        if (waiver != null)
        {
            _context.Waivers.Remove(waiver);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool WaiverExists(int id)
    {
        return _context.Waivers.Any(e => e.Uid == id);
    }

    private static void NormalizeNonNullableDbFields(Waiver waiver)
    {
        waiver.CustomerNo ??= string.Empty;
        waiver.CustomerName ??= string.Empty;
        waiver.WaiveOffType ??= string.Empty;
    }
}
