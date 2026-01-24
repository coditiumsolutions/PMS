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

    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveModule = "Plots";
        await LoadDashboardData();
        return View("Dashboard");
    }

    public async Task<IActionResult> Dashboard()
    {
        ViewBag.ActiveModule = "Plots";
        await LoadDashboardData();
        return View();
    }

    private async Task LoadDashboardData()
    {
        var plots = await _context.InventoryDetails.AsNoTracking().ToListAsync();

        var categoryGroups = plots
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "Unknown" : p.Category.Trim())
            .OrderByDescending(g => g.Count())
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .ToList();

        var statusGroups = plots
            .GroupBy(p => string.IsNullOrWhiteSpace(p.AllotmentStatus) ? "Not Specified" : p.AllotmentStatus.Trim())
            .OrderByDescending(g => g.Count())
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .ToList();

        var projectGroups = plots
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Project) ? "Unknown" : p.Project.Trim())
            .OrderByDescending(g => g.Count())
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .ToList();

        ViewBag.CategoryLabels = System.Text.Json.JsonSerializer.Serialize(categoryGroups.Select(c => c.Label));
        ViewBag.CategoryCounts = System.Text.Json.JsonSerializer.Serialize(categoryGroups.Select(c => c.Count));
        ViewBag.StatusLabels = System.Text.Json.JsonSerializer.Serialize(statusGroups.Select(s => s.Label));
        ViewBag.StatusCounts = System.Text.Json.JsonSerializer.Serialize(statusGroups.Select(s => s.Count));
        ViewBag.ProjectLabels = System.Text.Json.JsonSerializer.Serialize(projectGroups.Select(p => p.Label));
        ViewBag.ProjectCounts = System.Text.Json.JsonSerializer.Serialize(projectGroups.Select(p => p.Count));
    }

    // GET: Plots/Summary
    public async Task<IActionResult> Summary()
    {
        ViewBag.ActiveModule = "Plots";
        
        var plots = await _context.InventoryDetails.ToListAsync();
        
        // Total plots
        ViewBag.TotalPlots = plots.Count;
        
        // Plots by Category
        var plotsByCategory = plots
            .Where(p => !string.IsNullOrEmpty(p.Category))
            .GroupBy(p => p.Category)
            .Select(g => new KeyValuePair<string, int>(g.Key ?? "Unknown", g.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();
        ViewBag.PlotsByCategory = plotsByCategory;
        
        // Plots by Allotment Status
        var plotsByAllotmentStatus = plots
            .GroupBy(p => !string.IsNullOrEmpty(p.AllotmentStatus) ? p.AllotmentStatus : "Not Specified")
            .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();
        ViewBag.PlotsByAllotmentStatus = plotsByAllotmentStatus;
        
        // Plots by Project
        var plotsByProject = plots
            .Where(p => !string.IsNullOrEmpty(p.Project))
            .GroupBy(p => p.Project ?? "Unknown")
            .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .ToList();
        ViewBag.PlotsByProject = plotsByProject;
        
        // Plots by Unit Type
        var plotsByUnitType = plots
            .Where(p => !string.IsNullOrEmpty(p.UnitType))
            .GroupBy(p => p.UnitType)
            .Select(g => new KeyValuePair<string, int>(g.Key ?? "Unknown", g.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();
        ViewBag.PlotsByUnitType = plotsByUnitType;
        
        // Plots by Development Status
        var plotsByDevelopmentStatus = plots
            .Where(p => !string.IsNullOrEmpty(p.DevelopmentStatus))
            .GroupBy(p => p.DevelopmentStatus)
            .Select(g => new KeyValuePair<string, int>(g.Key ?? "Unknown", g.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();
        ViewBag.PlotsByDevelopmentStatus = plotsByDevelopmentStatus;
        
        // Plots by Construction Status
        var plotsByConstructionStatus = plots
            .Where(p => !string.IsNullOrEmpty(p.ConstStatus))
            .GroupBy(p => p.ConstStatus)
            .Select(g => new KeyValuePair<string, int>(g.Key ?? "Unknown", g.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();
        ViewBag.PlotsByConstructionStatus = plotsByConstructionStatus;
        
        // Available plots count
        ViewBag.AvailablePlots = plots.Count(p => p.AllotmentStatus?.ToLower() == "available");
        
        // Reserved plots count
        ViewBag.ReservedPlots = plots.Count(p => p.AllotmentStatus?.ToLower() == "reserved");
        
        // Allotted plots count
        ViewBag.AllottedPlots = plots.Count(p => p.AllotmentStatus?.ToLower() == "allotted");
        
        // Residential plots count
        ViewBag.ResidentialPlots = plots.Count(p => p.Category?.ToLower() == "residential");
        
        // Commercial plots count
        ViewBag.CommercialPlots = plots.Count(p => p.Category?.ToLower() == "commercial");
        
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
                       i.AllotmentStatus.ToLower() == "reserved")
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

