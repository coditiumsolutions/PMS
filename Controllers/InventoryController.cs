using CsvHelper;
using ExcelDataReader;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Models;
using PMS.Web.ViewModels;
using System.Globalization;
using System.Text;
using System.IO;

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

        if (string.IsNullOrWhiteSpace(inventoryDetail.Project)) ModelState.AddModelError("Project", "Project is required.");
        if (string.IsNullOrWhiteSpace(inventoryDetail.SubProject)) ModelState.AddModelError("SubProject", "Sub Project is required.");
        if (string.IsNullOrWhiteSpace(inventoryDetail.Sector)) ModelState.AddModelError("Sector", "Sector is required.");
        if (string.IsNullOrWhiteSpace(inventoryDetail.Block)) ModelState.AddModelError("Block", "Block is required.");
        if (string.IsNullOrWhiteSpace(inventoryDetail.Street)) ModelState.AddModelError("Street", "Street is required.");
        if (string.IsNullOrWhiteSpace(inventoryDetail.PlotNo)) ModelState.AddModelError("PlotNo", "Plot No is required.");

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

    // GET: Inventory/BulkUpload
    public IActionResult BulkUpload()
    {
        ViewBag.ActiveModule = "Inventory";
        return View(new InventoryBulkImportViewModel());
    }

    // GET: Inventory/DownloadSampleTemplate
    public IActionResult DownloadSampleTemplate()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Project,SubProject,Sector,Block,Street,PlotNo,Category,UnitSize,UnitType,FloorNo,UnitNo");
        builder.AppendLine("Northspire,Northspire East,Sector 1,A-1,Main Boulevard,Plot-101,Residential,5 Marla,Plot,,");
        builder.AppendLine("Northspire,Northspire West,Sector 3,B-3,Sunset Avenue,Plot-202,Commercial,8 Marla,Apartment,2,201");
        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        return File(bytes, "text/csv", "InventoryTemplate.csv");
    }

    // POST: Inventory/BulkUpload
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkUpload(IFormFile? importFile)
    {
        ViewBag.ActiveModule = "Inventory";
        var result = new InventoryBulkImportViewModel();

        if (importFile == null || importFile.Length == 0)
        {
            ModelState.AddModelError("importFile", "Please upload a CSV or Excel file containing the inventory data.");
            return View(result);
        }

        var extension = Path.GetExtension(importFile.FileName)?.ToLowerInvariant();
        if (extension is null or "" || (extension != ".csv" && extension != ".xls" && extension != ".xlsx"))
        {
            ModelState.AddModelError("importFile", "Only .csv, .xls, and .xlsx files are supported.");
            return View(result);
        }

        List<Dictionary<string, string>> rows;
        using var memory = new MemoryStream();
        await importFile.CopyToAsync(memory);

        try
        {
            if (extension == ".csv")
            {
                rows = ReadRowsFromCsv(memory);
            }
            else
            {
                rows = ReadRowsFromExcel(memory);
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("importFile", $"Unable to parse the file: {ex.Message}");
            return View(result);
        }

        if (!rows.Any())
        {
            ModelState.AddModelError("importFile", "The uploaded file does not contain any data rows.");
            return View(result);
        }

        var existingKeys = new HashSet<string>(_context.InventoryDetails
            .Select(i => CreateInventoryKey(i.Project, i.SubProject, i.PlotNo))
            .Where(k => !string.IsNullOrWhiteSpace(k)),
            StringComparer.OrdinalIgnoreCase);

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var toInsert = new List<InventoryDetail>();
        var errors = new List<string>();
        int duplicates = 0;
        int failed = 0;
        int rowNumber = 1;

        foreach (var row in rows)
        {
            rowNumber++;
            var project = GetValue(row, "Project");
            var subProject = GetValue(row, "SubProject");
            var sector = GetValue(row, "Sector");
            var block = GetValue(row, "Block");
            var street = GetValue(row, "Street");
            var plotNo = GetValue(row, "PlotNo");

            if (string.IsNullOrWhiteSpace(project) ||
                string.IsNullOrWhiteSpace(subProject) ||
                string.IsNullOrWhiteSpace(sector) ||
                string.IsNullOrWhiteSpace(block) ||
                string.IsNullOrWhiteSpace(street) ||
                string.IsNullOrWhiteSpace(plotNo))
            {
                failed++;
                errors.Add($"Row {rowNumber}: Required fields (Project/SubProject/Sector/Block/Street/Plot No) must not be empty.");
                continue;
            }

            var key = CreateInventoryKey(project, subProject, plotNo);
            if (existingKeys.Contains(key) || seenKeys.Contains(key))
            {
                duplicates++;
                continue;
            }

            var inventory = new InventoryDetail
            {
                Project = project,
                SubProject = subProject,
                Sector = sector,
                Block = block,
                Street = street,
                PlotNo = plotNo,
                Category = GetValue(row, "Category"),
                UnitSize = GetValue(row, "UnitSize"),
                UnitType = GetValue(row, "UnitType"),
                FloorNo = GetValue(row, "FloorNo"),
                UnitNo = GetValue(row, "UnitNo"),
                DevelopmentStatus = string.IsNullOrWhiteSpace(GetValue(row, "DevelopmentStatus")) ? "No" : GetValue(row, "DevelopmentStatus"),
                ConstStatus = string.IsNullOrWhiteSpace(GetValue(row, "ConstStatus")) ? "Vacant" : GetValue(row, "ConstStatus"),
                AllotmentStatus = string.IsNullOrWhiteSpace(GetValue(row, "AllotmentStatus")) ? "Available" : GetValue(row, "AllotmentStatus"),
                CreationDate = ParseDate(GetValue(row, "CreationDate")) ?? DateTime.Today
            };

            toInsert.Add(inventory);
            seenKeys.Add(key);
        }

        if (toInsert.Any())
        {
            await _context.InventoryDetails.AddRangeAsync(toInsert);
            await _context.SaveChangesAsync();
            result.Inserted = toInsert.Count;
        }

        result.Duplicates = duplicates;
        result.Failed = failed;
        result.Errors = errors;
        result.StatusMessage = $"Processed {rows.Count} row(s): {result.Inserted} inserted, {duplicates} duplicates, {failed} failed.";

        return View("BulkUpload", result);
    }

    private static string CreateInventoryKey(string? project, string? subProject, string? plotNo)
    {
        return $"{project?.Trim() ?? string.Empty}|{subProject?.Trim() ?? string.Empty}|{plotNo?.Trim() ?? string.Empty}";
    }

    private static string GetValue(Dictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out var value) ? value : string.Empty;
    }

    private static DateTime? ParseDate(string value)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed.Date;
        }
        return null;
    }

    private static List<Dictionary<string, string>> ReadRowsFromCsv(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var rows = new List<Dictionary<string, string>>();


        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in csv.HeaderRecord!)
            {
                var field = csv.GetField(header);
                record[header] = field?.Trim() ?? string.Empty;
            }
            rows.Add(record);
        }

        return rows;
    }

    private static List<Dictionary<string, string>> ReadRowsFromExcel(Stream stream)
    {
        stream.Position = 0;
        System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var dataset = reader.AsDataSet();

        if (dataset.Tables.Count == 0)
        {
            return new();
        }

        var table = dataset.Tables[0];
        if (table.Rows.Count < 1)
        {
            return new();
        }

        var headers = table.Rows[0].ItemArray
            .Select(x => x?.ToString()?.Trim() ?? string.Empty)
            .ToList();

        var rows = new List<Dictionary<string, string>>();

        for (var i = 1; i < table.Rows.Count; i++)
        {
            var rowDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var j = 0; j < table.Columns.Count; j++)
            {
                var header = j < headers.Count ? headers[j] : $"Column{j}";
                if (string.IsNullOrWhiteSpace(header))
                {
                    header = $"Column{j}";
                }

                var value = table.Rows[i][j]?.ToString()?.Trim() ?? string.Empty;
                rowDict[header] = value;
            }
            rows.Add(rowDict);
        }

        return rows;
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

        if (string.IsNullOrWhiteSpace(inventoryDetail.Project)) ModelState.AddModelError("Project", "Project is required.");
        if (string.IsNullOrWhiteSpace(inventoryDetail.SubProject)) ModelState.AddModelError("SubProject", "Sub Project is required.");
        if (string.IsNullOrWhiteSpace(inventoryDetail.Sector)) ModelState.AddModelError("Sector", "Sector is required.");
        if (string.IsNullOrWhiteSpace(inventoryDetail.Block)) ModelState.AddModelError("Block", "Block is required.");
        if (string.IsNullOrWhiteSpace(inventoryDetail.Street)) ModelState.AddModelError("Street", "Street is required.");
        if (string.IsNullOrWhiteSpace(inventoryDetail.PlotNo)) ModelState.AddModelError("PlotNo", "Plot No is required.");

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
