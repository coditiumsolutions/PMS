using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Models;

namespace PMS.Web.Controllers;

[Route("Setup/Configurations")]
public class ConfigurationController : Controller
{
    private readonly PMSDbContext _context;

    public ConfigurationController(PMSDbContext context)
    {
        _context = context;
    }

    // GET: Setup/Configurations
    [HttpGet]
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveModule = "Setup";
        return View(await _context.Configurations.ToListAsync());
    }

    // GET: Setup/Configurations/Details/5
    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int? id)
    {
        ViewBag.ActiveModule = "Setup";
        if (id == null)
        {
            return NotFound();
        }

        var configuration = await _context.Configurations
            .FirstOrDefaultAsync(m => m.Id == id);
        if (configuration == null)
        {
            return NotFound();
        }

        return View(configuration);
    }

    // GET: Setup/Configurations/Create
    [HttpGet("Create")]
    public IActionResult Create()
    {
        ViewBag.ActiveModule = "Setup";
        return View();
    }

    // POST: Setup/Configurations/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,ConfigKey,ConfigValue,IsActive")] Configuration configuration)
    {
        ViewBag.ActiveModule = "Setup";
        if (ModelState.IsValid)
        {
            _context.Add(configuration);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(configuration);
    }

    // GET: Setup/Configurations/Edit/5
    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int? id)
    {
        ViewBag.ActiveModule = "Setup";
        if (id == null)
        {
            return NotFound();
        }

        var configuration = await _context.Configurations.FindAsync(id);
        if (configuration == null)
        {
            return NotFound();
        }
        return View(configuration);
    }

    // POST: Setup/Configurations/Edit/5
    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ConfigKey,ConfigValue,IsActive")] Configuration configuration)
    {
        ViewBag.ActiveModule = "Setup";
        if (id != configuration.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(configuration);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ConfigurationExists(configuration.Id))
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
        return View(configuration);
    }

    // GET: Setup/Configurations/Delete/5
    [HttpGet("Delete/{id}")]
    public async Task<IActionResult> Delete(int? id)
    {
        ViewBag.ActiveModule = "Setup";
        if (id == null)
        {
            return NotFound();
        }

        var configuration = await _context.Configurations
            .FirstOrDefaultAsync(m => m.Id == id);
        if (configuration == null)
        {
            return NotFound();
        }

        return View(configuration);
    }

    // POST: Setup/Configurations/Delete/5
    [HttpPost("Delete/{id}"), ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        ViewBag.ActiveModule = "Setup";
        var configuration = await _context.Configurations.FindAsync(id);
        if (configuration != null)
        {
            _context.Configurations.Remove(configuration);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ConfigurationExists(int id)
    {
        return _context.Configurations.Any(e => e.Id == id);
    }
}
