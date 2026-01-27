using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Models;

namespace PMS.Web.Controllers;

public class TransfersController : Controller
{
    private readonly PMSDbContext _context;

    public TransfersController(PMSDbContext context)
    {
        _context = context;
    }
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

    public IActionResult Payments()
    {
        ViewBag.ActiveModule = "Transfers";
        return View();
    }

    public IActionResult TransferFees()
    {
        ViewBag.ActiveModule = "Transfers";
        return View();
    }

    // GET: Transfers/Customers
    public async Task<IActionResult> Customers()
    {
        ViewBag.ActiveModule = "Transfers";
        return View(await _context.Customers.OrderBy(c => c.CustomerNo).ToListAsync());
    }

    // GET: Transfers/AllTransfers
    public async Task<IActionResult> AllTransfers()
    {
        ViewBag.ActiveModule = "Transfers";
        return View(await _context.Transfers.OrderByDescending(t => t.uId).ToListAsync());
    }

    // GET: Transfers/Details/5
    public async Task<IActionResult> TransferDetails(int? id)
    {
        ViewBag.ActiveModule = "Transfers";
        if (id == null)
        {
            return NotFound();
        }

        var transfer = await _context.Transfers
            .FirstOrDefaultAsync(m => m.uId == id);
        if (transfer == null)
        {
            return NotFound();
        }

        return View(transfer);
    }

    // GET: Transfers/Create
    public async Task<IActionResult> Create(string? customerNo = null)
    {
        ViewBag.ActiveModule = "Transfers";
        var transferTypes = await _context.Configurations
            .Where(c => c.ConfigKey == "TransferType" && c.IsActive)
            .OrderBy(c => c.ConfigValue)
            .Select(c => c.ConfigValue)
            .ToListAsync();
        ViewBag.TransferTypes = new SelectList(transferTypes);
        var customerNumbers = await _context.Customers
            .OrderBy(c => c.CustomerNo)
            .Select(c => c.CustomerNo)
            .ToListAsync();
        ViewBag.CustomerNumbers = new SelectList(customerNumbers);
        var model = new Transfer { CustomerNo = customerNo ?? string.Empty };
        return View(model);
    }

    // POST: Transfers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CustomerNo,TransferType,NatureOfTransfer,InitiatingOffice,OwnerType,BuyerName,BuyerFatherName,BuyerCnic,BuyerContactNo,BuyerEmail,BuyerGender,BuyerPresAddress,BuyerPremAddress,BuyerPresCity,BuyerPremCity,BuyerPresCountry,BuyerPremCountry,CreationDate,CreatedBy,SellerInfo,NdcExist,NdcAmount,NdcDetail")] Transfer transfer)
    {
        ViewBag.ActiveModule = "Transfers";
        
        if (ModelState.IsValid)
        {
            if (string.IsNullOrWhiteSpace(transfer.CreationDate))
            {
                transfer.CreationDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            
            _context.Add(transfer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AllTransfers));
        }
        var transferTypesRepop = await _context.Configurations
            .Where(c => c.ConfigKey == "TransferType" && c.IsActive)
            .OrderBy(c => c.ConfigValue)
            .Select(c => c.ConfigValue)
            .ToListAsync();
        ViewBag.TransferTypes = new SelectList(transferTypesRepop);
        var customerNumbersRepop = await _context.Customers
            .OrderBy(c => c.CustomerNo)
            .Select(c => c.CustomerNo)
            .ToListAsync();
        ViewBag.CustomerNumbers = new SelectList(customerNumbersRepop);
        return View(transfer);
    }

    // GET: Transfers/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        ViewBag.ActiveModule = "Transfers";
        if (id == null)
        {
            return NotFound();
        }

        var transfer = await _context.Transfers.FindAsync(id);
        if (transfer == null)
        {
            return NotFound();
        }
        return View(transfer);
    }

    // POST: Transfers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("uId,CustomerNo,TransferType,NatureOfTransfer,InitiatingOffice,OwnerType,BuyerName,BuyerFatherName,BuyerCnic,BuyerContactNo,BuyerEmail,BuyerGender,BuyerPresAddress,BuyerPremAddress,BuyerPresCity,BuyerPremCity,BuyerPresCountry,BuyerPremCountry,CreationDate,CreatedBy,SellerInfo,NdcExist,NdcAmount,NdcDetail")] Transfer transfer)
    {
        ViewBag.ActiveModule = "Transfers";
        if (id != transfer.uId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(transfer);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TransferExists(transfer.uId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(AllTransfers));
        }
        return View(transfer);
    }

    // GET: Transfers/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        ViewBag.ActiveModule = "Transfers";
        if (id == null)
        {
            return NotFound();
        }

        var transfer = await _context.Transfers
            .FirstOrDefaultAsync(m => m.uId == id);
        if (transfer == null)
        {
            return NotFound();
        }

        return View(transfer);
    }

    // POST: Transfers/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        ViewBag.ActiveModule = "Transfers";
        var transfer = await _context.Transfers.FindAsync(id);
        if (transfer != null)
        {
            _context.Transfers.Remove(transfer);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(AllTransfers));
    }

    private bool TransferExists(int id)
    {
        return _context.Transfers.Any(e => e.uId == id);
    }
}

