using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Models;
using System.Text.Json;

namespace PMS.Web.Controllers;

public class RegistrationController : Controller
{
    private readonly PMSDbContext _context;

    public RegistrationController(PMSDbContext context)
    {
        _context = context;
    }

    // GET: Registration
    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveModule = "Registration";
        
        var registrations = await _context.Registrations
            .Include(r => r.Project)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        
        // Get all registration IDs that already have customers
        var registrationIdsWithCustomers = await _context.Customers
            .Where(c => c.RegistrationID.HasValue)
            .Select(c => c.RegistrationID.Value)
            .Distinct()
            .ToListAsync();
        
        ViewBag.RegistrationIdsWithCustomers = registrationIdsWithCustomers;
        
        return View(registrations);
    }

    // GET: Registration/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        ViewBag.ActiveModule = "Registration";
        await LoadDashboardData();
        return View();
    }

    // GET: Registration/Summary
    public async Task<IActionResult> Summary()
    {
        ViewBag.ActiveModule = "Registration";
        
        var registrations = await _context.Registrations.AsNoTracking().ToListAsync();
        
        ViewBag.TotalRegistrations = registrations.Count;
        ViewBag.PendingRegistrations = registrations.Count(r => r.Status?.ToLower() == "pending");
        ViewBag.ApprovedRegistrations = registrations.Count(r => r.Status?.ToLower() == "approved");
        ViewBag.RejectedRegistrations = registrations.Count(r => r.Status?.ToLower() == "rejected");
        ViewBag.CompletedRegistrations = registrations.Count(r => r.Status?.ToLower() == "completed");
        
        ViewBag.StatusGroups = registrations
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Status) ? "Unknown" : r.Status.Trim())
            .OrderByDescending(g => g.Count())
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToList();

        ViewBag.ProjectGroups = registrations
            .Where(r => r.ProjectID.HasValue)
            .GroupBy(r => r.ProjectID)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new { ProjectID = g.Key, Count = g.Count() })
            .ToList();

        return View();
    }

    // GET: Registration/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        ViewBag.ActiveModule = "Registration";
        if (id == null)
        {
            return NotFound();
        }

        var registration = await _context.Registrations
            .FirstOrDefaultAsync(m => m.RegID == id);
        if (registration == null)
        {
            return NotFound();
        }

        return View(registration);
    }

    // GET: Registration/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.ActiveModule = "Registration";
        
        ViewBag.ProjectOptions = await _context.Projects
            .OrderBy(p => p.ProjectName)
            .ToListAsync();

        var model = new Registration
        {
            Status = "Pending",
            CreatedAt = DateTime.Now
        };

        return View(model);
    }

    // POST: Registration/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FullName,CNIC,Phone,Email,ProjectID,RequestedSize,Remarks,Status")] Registration registration)
    {
        ViewBag.ActiveModule = "Registration";
        
        ViewBag.ProjectOptions = await _context.Projects
            .OrderBy(p => p.ProjectName)
            .ToListAsync();

        registration.CreatedAt = DateTime.Now;
        if (string.IsNullOrWhiteSpace(registration.Status))
        {
            registration.Status = "Pending";
        }

        if (ModelState.IsValid)
        {
            _context.Add(registration);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(registration);
    }

    // GET: Registration/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        ViewBag.ActiveModule = "Registration";
        if (id == null)
        {
            return NotFound();
        }

        var registration = await _context.Registrations.FindAsync(id);
        if (registration == null)
        {
            return NotFound();
        }

        ViewBag.ProjectOptions = await _context.Projects
            .OrderBy(p => p.ProjectName)
            .ToListAsync();

        return View(registration);
    }

    // POST: Registration/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("RegID,FullName,CNIC,Phone,Email,ProjectID,RequestedSize,Remarks,CreatedAt,Status")] Registration registration)
    {
        ViewBag.ActiveModule = "Registration";
        
        if (id != registration.RegID)
        {
            return NotFound();
        }

        ViewBag.ProjectOptions = await _context.Projects
            .OrderBy(p => p.ProjectName)
            .ToListAsync();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(registration);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RegistrationExists(registration.RegID))
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
        return View(registration);
    }

    private bool RegistrationExists(int id)
    {
        return _context.Registrations.Any(e => e.RegID == id);
    }

    private async Task LoadDashboardData()
    {
        var registrations = await _context.Registrations.AsNoTracking().ToListAsync();
        
        var statusGroups = registrations
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Status) ? "Unknown" : r.Status.Trim())
            .OrderByDescending(g => g.Count())
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .ToList();

        var projectGroups = registrations
            .Where(r => r.ProjectID.HasValue)
            .Join(_context.Projects,
                r => r.ProjectID,
                p => p.Id,
                (r, p) => p.ProjectName)
            .GroupBy(pn => string.IsNullOrWhiteSpace(pn) ? "Unknown" : pn.Trim())
            .OrderByDescending(g => g.Count())
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .Take(10)
            .ToList();

        ViewBag.StatusLabels = JsonSerializer.Serialize(statusGroups.Select(s => s.Label));
        ViewBag.StatusCounts = JsonSerializer.Serialize(statusGroups.Select(s => s.Count));
        ViewBag.ProjectLabels = JsonSerializer.Serialize(projectGroups.Select(p => p.Label));
        ViewBag.ProjectCounts = JsonSerializer.Serialize(projectGroups.Select(p => p.Count));
        
        ViewBag.TotalRegistrations = registrations.Count;
        ViewBag.PendingCount = registrations.Count(r => r.Status?.ToLower() == "pending");
        ViewBag.ApprovedCount = registrations.Count(r => r.Status?.ToLower() == "approved");
        ViewBag.RejectedCount = registrations.Count(r => r.Status?.ToLower() == "rejected");
    }
}
