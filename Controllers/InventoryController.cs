using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Models;

namespace PMS.Web.Controllers;

public class InventoryController : Controller
{
    private readonly PMSDbContext _context;

    public InventoryController(PMSDbContext context)
    {
        _context = context;
    }

    // GET: Inventory
    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveModule = "Inventory";
        return View(await _context.InventoryDetails.OrderByDescending(i => i.UID).ToListAsync());
    }

    // GET: Inventory/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.ActiveModule = "Inventory";

        ViewBag.ProjectOptions = await _context.Projects
            .Select(p => p.ProjectName)
            .Where(pn => !string.IsNullOrWhiteSpace(pn))
            .Distinct()
            .OrderBy(pn => pn)
            .ToListAsync();

        ViewBag.SubProjectOptions = await _context.Projects
            .Select(p => p.SubProject)
            .Where(sp => !string.IsNullOrWhiteSpace(sp))
            .Distinct()
            .OrderBy(sp => sp)
            .ToListAsync();

        var model = new InventoryDetail
        {
            DevelopmentStatus = "No",
            ConstStatus = "Vacant",
            AllotmentStatus = "Available",
            CreationDate = DateTime.Today
        };

        return View(model);
    }

    // POST: Inventory/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Project,SubProject,Sector,Block,Street,PlotNo,Category,UnitSize,UnitType,DevelopmentStatus,ConstStatus,AllotmentStatus,FloorNo,UnitNo,CreationDate")] InventoryDetail inventoryDetail)
    {
        ViewBag.ActiveModule = "Inventory";

        ViewBag.ProjectOptions = await _context.Projects
            .Select(p => p.ProjectName)
            .Where(pn => !string.IsNullOrWhiteSpace(pn))
            .Distinct()
            .OrderBy(pn => pn)
            .ToListAsync();

        ViewBag.SubProjectOptions = await _context.Projects
            .Select(p => p.SubProject)
            .Where(sp => !string.IsNullOrWhiteSpace(sp))
            .Distinct()
            .OrderBy(sp => sp)
            .ToListAsync();

        inventoryDetail.CreationDate ??= DateTime.Today;
        inventoryDetail.DevelopmentStatus ??= "No";
        inventoryDetail.ConstStatus ??= "Vacant";
        inventoryDetail.AllotmentStatus ??= "Available";

        if (ModelState.IsValid)
        {
            _context.Add(inventoryDetail);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(inventoryDetail);
    }

    // GET: Inventory/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        ViewBag.ActiveModule = "Inventory";
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

    // GET: Inventory/Summary
    public async Task<IActionResult> Summary()
    {
        ViewBag.ActiveModule = "Inventory";

        var plots = await _context.InventoryDetails.ToListAsync();

        ViewBag.TotalPlots = plots.Count;

        var plotsByCategory = plots
            .Where(p => !string.IsNullOrEmpty(p.Category))
            .GroupBy(p => p.Category)
            .Select(g => new KeyValuePair<string, int>(g.Key ?? "Unknown", g.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();

        ViewBag.PlotsByCategory = plotsByCategory;

        var plotsByAllotmentStatus = plots
            .GroupBy(p => !string.IsNullOrEmpty(p.AllotmentStatus) ? p.AllotmentStatus : "Not Specified")
            .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();

        ViewBag.PlotsByAllotmentStatus = plotsByAllotmentStatus;

        var plotsByProject = plots
            .Where(p => !string.IsNullOrEmpty(p.Project))
            .GroupBy(p => p.Project ?? "Unknown")
            .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .ToList();

        ViewBag.PlotsByProject = plotsByProject;

        var plotsByUnitType = plots
            .Where(p => !string.IsNullOrEmpty(p.UnitType))
            .GroupBy(p => p.UnitType)
            .Select(g => new KeyValuePair<string, int>(g.Key ?? "Unknown", g.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();

        ViewBag.PlotsByUnitType = plotsByUnitType;

        var plotsByDevelopmentStatus = plots
            .Where(p => !string.IsNullOrEmpty(p.DevelopmentStatus))
            .GroupBy(p => p.DevelopmentStatus)
            .Select(g => new KeyValuePair<string, int>(g.Key ?? "Unknown", g.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();

        ViewBag.PlotsByDevelopmentStatus = plotsByDevelopmentStatus;

        var plotsByConstructionStatus = plots
            .Where(p => !string.IsNullOrEmpty(p.ConstStatus))
            .GroupBy(p => p.ConstStatus)
            .Select(g => new KeyValuePair<string, int>(g.Key ?? "Unknown", g.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();

        ViewBag.PlotsByConstructionStatus = plotsByConstructionStatus;

        ViewBag.AvailablePlots = plots.Count(p => p.AllotmentStatus?.ToLower() == "available");
        ViewBag.ReservedPlots = plots.Count(p => p.AllotmentStatus?.ToLower() == "reserved");
        ViewBag.AllottedPlots = plots.Count(p => p.AllotmentStatus?.ToLower() == "allotted");
        ViewBag.ResidentialPlots = plots.Count(p => p.Category?.ToLower() == "residential");
        ViewBag.CommercialPlots = plots.Count(p => p.Category?.ToLower() == "commercial");

        return View();
    }

    public IActionResult History()
    {
        ViewBag.ActiveModule = "Inventory";
        return View();
    }

    // GET: Inventory/Reserved
    public async Task<IActionResult> Reserved()
    {
        ViewBag.ActiveModule = "Inventory";
        var reservedPlots = await _context.InventoryDetails
            .Where(i => i.AllotmentStatus != null &&
                       i.AllotmentStatus.ToLower() == "reserved")
            .OrderByDescending(i => i.UID)
            .ToListAsync();
        return View(reservedPlots);
    }

    // GET: Inventory/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        ViewBag.ActiveModule = "Inventory";
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

    // GET: Inventory/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        ViewBag.ActiveModule = "Inventory";
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

    // POST: Inventory/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("UID,Project,SubProject,Sector,Block,Street,PlotNo,Category,UnitSize,UnitType,DevelopmentStatus,ConstStatus,AllotmentStatus,CustomerNo,AllotmentDate,AllottedBy,FloorNo,UnitNo,CreationDate")] InventoryDetail inventoryDetail)
    {
        ViewBag.ActiveModule = "Inventory";
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
            return RedirectToAction(nameof(Index));
        }
        return View(inventoryDetail);
    }

    // GET: Inventory/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        ViewBag.ActiveModule = "Inventory";
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

    // POST: Inventory/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        ViewBag.ActiveModule = "Inventory";
        var inventoryDetail = await _context.InventoryDetails.FindAsync(id);
        if (inventoryDetail != null)
        {
            _context.InventoryDetails.Remove(inventoryDetail);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // POST: Inventory/Search
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Search([FromBody] string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Json(new { success = false, message = "Please enter a search term" });
        }

        var trimmedSearch = searchTerm.Trim().ToLower();
        
        var inventory = await _context.InventoryDetails
            .Where(i => 
                (!string.IsNullOrEmpty(i.PlotNo) && i.PlotNo.ToLower().Contains(trimmedSearch)) ||
                (!string.IsNullOrEmpty(i.Project) && i.Project.ToLower().Contains(trimmedSearch)) ||
                (!string.IsNullOrEmpty(i.CustomerNo) && i.CustomerNo.ToLower().Contains(trimmedSearch)) ||
                (!string.IsNullOrEmpty(i.UnitNo) && i.UnitNo.ToLower().Contains(trimmedSearch)))
            .OrderByDescending(i => i.UID)
            .ToListAsync();

        return Json(new { success = true, inventory = inventory });
    }

    private bool InventoryDetailExists(int id)
    {
        return _context.InventoryDetails.Any(e => e.UID == id);
    }
}
