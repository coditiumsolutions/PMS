using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Models;

namespace PMS.Web.Controllers;

[Route("Refunds")]
public class RefundsController : Controller
{
    private readonly PMSDbContext _context;

    public RefundsController(PMSDbContext context)
    {
        _context = context;
    }

    // GET: Refunds/LookupCustomer?customerNo=CUST001
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
                fatherName = customer.FatherName,
                cnicNo = customer.Cnic,
                projectName = inventory?.Project ?? string.Empty,
                plotSize = inventory?.UnitSize ?? string.Empty,
                propertyDetail
            }
        });
    }

    // GET: Refunds
    [HttpGet]
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveModule = "Refunds";
        return View(await _context.Refunds.OrderByDescending(r => r.Uid).ToListAsync());
    }

    // GET: Refunds/Details/5
    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int? id)
    {
        ViewBag.ActiveModule = "Refunds";
        if (id == null)
        {
            return NotFound();
        }

        var refund = await _context.Refunds.FirstOrDefaultAsync(m => m.Uid == id);
        if (refund == null)
        {
            return NotFound();
        }

        return View(refund);
    }

    // GET: Refunds/Create
    [HttpGet("Create")]
    public IActionResult Create()
    {
        ViewBag.ActiveModule = "Refunds";
        return View(new Refund { CreatedDate = DateTime.Now, IsActive = true });
    }

    // POST: Refunds/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Uid,RefundType,CustomerNo,CustomerName,FatherName,CNICNo,TotalPaidAmount,RefundableAmount,Deduction,AmountToRefund,Detail,ProjectName,ApplicationNo,PlotSize,PropertyDetail,CreatedDate,CreatedBy,IsActive")] Refund refund)
    {
        ViewBag.ActiveModule = "Refunds";
        NormalizeNonNullableDbFields(refund);
        if (ModelState.IsValid)
        {
            if (!refund.CreatedDate.HasValue)
            {
                refund.CreatedDate = DateTime.Now;
            }

            _context.Add(refund);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(refund);
    }

    // GET: Refunds/Edit/5
    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int? id)
    {
        ViewBag.ActiveModule = "Refunds";
        if (id == null)
        {
            return NotFound();
        }

        var refund = await _context.Refunds.FindAsync(id);
        if (refund == null)
        {
            return NotFound();
        }
        return View(refund);
    }

    // POST: Refunds/Edit/5
    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Uid,RefundType,CustomerNo,CustomerName,FatherName,CNICNo,TotalPaidAmount,RefundableAmount,Deduction,AmountToRefund,Detail,ProjectName,ApplicationNo,PlotSize,PropertyDetail,CreatedDate,CreatedBy,IsActive")] Refund refund)
    {
        ViewBag.ActiveModule = "Refunds";
        NormalizeNonNullableDbFields(refund);
        if (id != refund.Uid)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(refund);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RefundExists(refund.Uid))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(refund);
    }

    // GET: Refunds/Delete/5
    [HttpGet("Delete/{id}")]
    public async Task<IActionResult> Delete(int? id)
    {
        ViewBag.ActiveModule = "Refunds";
        if (id == null)
        {
            return NotFound();
        }

        var refund = await _context.Refunds.FirstOrDefaultAsync(m => m.Uid == id);
        if (refund == null)
        {
            return NotFound();
        }

        return View(refund);
    }

    // POST: Refunds/Delete/5
    [HttpPost("Delete/{id}"), ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        ViewBag.ActiveModule = "Refunds";
        var refund = await _context.Refunds.FindAsync(id);
        if (refund != null)
        {
            _context.Refunds.Remove(refund);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool RefundExists(int id)
    {
        return _context.Refunds.Any(e => e.Uid == id);
    }

    private static void NormalizeNonNullableDbFields(Refund refund)
    {
        refund.ApplicationNo ??= string.Empty;
        refund.ProjectName ??= string.Empty;
        refund.Detail ??= string.Empty;
    }
}
