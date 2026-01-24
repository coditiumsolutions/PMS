using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Models;

namespace PMS.Web.Controllers;

public class PlotsController : Controller
{
    private readonly PMSDbContext _context;

    public PlotsController(PMSDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        ViewBag.ActiveModule = "Plots";
        return View();
    }

    public IActionResult Dashboard()
    {
        ViewBag.ActiveModule = "Plots";
        return View();
    }

    public IActionResult Summary()
    {
        ViewBag.ActiveModule = "Plots";
        return View();
    }

    public IActionResult History()
    {
        ViewBag.ActiveModule = "Plots";
        return View();
    }

    // GET: Plots/AllPlots
    public async Task<IActionResult> AllPlots()
    {
        ViewBag.ActiveModule = "Plots";
        return View(await _context.InventoryDetails.OrderByDescending(i => i.UID).ToListAsync());
    }

    // GET: Plots/Reserved
    public async Task<IActionResult> Reserved()
    {
        ViewBag.ActiveModule = "Plots";
        var reservedPlots = await _context.InventoryDetails
            .Where(i => i.AllotmentStatus != null && 
                       (i.AllotmentStatus.ToLower() == "reserved" || 
                        i.AllotmentStatus.ToLower() == "allotted"))
            .OrderByDescending(i => i.UID)
            .ToListAsync();
        return View(reservedPlots);
    }

    // GET: Plots/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        ViewBag.ActiveModule = "Plots";
        if (id == null)
        {
            return NotFound();
        }

        var inventoryDetail = await _context.InventoryDetails
            .FirstOrDefaultAsync(m => m.UID == id);
        if (inventoryDetail == null)
        {
            return NotFound();
        }

        return View(inventoryDetail);
    }

    // GET: Plots/Create
    public IActionResult Create()
    {
        ViewBag.ActiveModule = "Plots";
        return View();
    }

    // POST: Plots/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Project,SubProject,Sector,Block,Street,PlotNo,Category,UnitSize,UnitType,DevelopmentStatus,ConstStatus,AllotmentStatus,CustomerNo,AllotmentDate,AllottedBy,FloorNo,UnitNo,CreationDate")] InventoryDetail inventoryDetail)
    {
        ViewBag.ActiveModule = "Plots";
        if (ModelState.IsValid)
        {
            _context.Add(inventoryDetail);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AllPlots));
        }
        return View(inventoryDetail);
    }

    // GET: Plots/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        ViewBag.ActiveModule = "Plots";
        if (id == null)
        {
            return NotFound();
        }

        var inventoryDetail = await _context.InventoryDetails.FindAsync(id);
        if (inventoryDetail == null)
        {
            return NotFound();
        }
        return View(inventoryDetail);
    }

    // POST: Plots/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("UID,Project,SubProject,Sector,Block,Street,PlotNo,Category,UnitSize,UnitType,DevelopmentStatus,ConstStatus,AllotmentStatus,CustomerNo,AllotmentDate,AllottedBy,FloorNo,UnitNo,CreationDate")] InventoryDetail inventoryDetail)
    {
        ViewBag.ActiveModule = "Plots";
        if (id != inventoryDetail.UID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(inventoryDetail);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InventoryDetailExists(inventoryDetail.UID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(AllPlots));
        }
        return View(inventoryDetail);
    }

    // GET: Plots/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        ViewBag.ActiveModule = "Plots";
        if (id == null)
        {
            return NotFound();
        }

        var inventoryDetail = await _context.InventoryDetails
            .FirstOrDefaultAsync(m => m.UID == id);
        if (inventoryDetail == null)
        {
            return NotFound();
        }

        return View(inventoryDetail);
    }

    // POST: Plots/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        ViewBag.ActiveModule = "Plots";
        var inventoryDetail = await _context.InventoryDetails.FindAsync(id);
        if (inventoryDetail != null)
        {
            _context.InventoryDetails.Remove(inventoryDetail);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(AllPlots));
    }

    private bool InventoryDetailExists(int id)
    {
        return _context.InventoryDetails.Any(e => e.UID == id);
    }
}

