using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Models;

namespace PMS.Web.Controllers;

[Route("Setup/MultiConfigs")]
public class MultiConfigsController : Controller
{
    private readonly PMSDbContext _context;

    public MultiConfigsController(PMSDbContext context)
    {
        _context = context;
    }

    // GET: Setup/MultiConfigs
    [HttpGet]
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveModule = "Setup";
        return View(await _context.MultiConfigs.OrderBy(m => m.UId).ToListAsync());
    }

    // GET: Setup/MultiConfigs/Details/5
    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int? id)
    {
        ViewBag.ActiveModule = "Setup";
        if (id == null)
        {
            return NotFound();
        }

        var multiConfig = await _context.MultiConfigs
            .FirstOrDefaultAsync(m => m.UId == id);
        if (multiConfig == null)
        {
            return NotFound();
        }

        return View(multiConfig);
    }

    // GET: Setup/MultiConfigs/Create
    [HttpGet("Create")]
    public IActionResult Create()
    {
        ViewBag.ActiveModule = "Setup";
        return View();
    }

    // POST: Setup/MultiConfigs/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("UId,ConfigKey,ConfigValue")] MultiConfig multiConfig)
    {
        ViewBag.ActiveModule = "Setup";
        if (ModelState.IsValid)
        {
            _context.Add(multiConfig);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(multiConfig);
    }

    // GET: Setup/MultiConfigs/Edit/5
    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int? id)
    {
        ViewBag.ActiveModule = "Setup";
        if (id == null)
        {
            return NotFound();
        }

        var multiConfig = await _context.MultiConfigs.FindAsync(id);
        if (multiConfig == null)
        {
            return NotFound();
        }
        return View(multiConfig);
    }

    // POST: Setup/MultiConfigs/Edit/5
    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("UId,ConfigKey,ConfigValue")] MultiConfig multiConfig)
    {
        ViewBag.ActiveModule = "Setup";
        if (id != multiConfig.UId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(multiConfig);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MultiConfigExists(multiConfig.UId))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(multiConfig);
    }

    // GET: Setup/MultiConfigs/Delete/5
    [HttpGet("Delete/{id}")]
    public async Task<IActionResult> Delete(int? id)
    {
        ViewBag.ActiveModule = "Setup";
        if (id == null)
        {
            return NotFound();
        }

        var multiConfig = await _context.MultiConfigs
            .FirstOrDefaultAsync(m => m.UId == id);
        if (multiConfig == null)
        {
            return NotFound();
        }

        return View(multiConfig);
    }

    // POST: Setup/MultiConfigs/Delete/5
    [HttpPost("Delete/{id}"), ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        ViewBag.ActiveModule = "Setup";
        var multiConfig = await _context.MultiConfigs.FindAsync(id);
        if (multiConfig != null)
        {
            _context.MultiConfigs.Remove(multiConfig);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool MultiConfigExists(int id)
    {
        return _context.MultiConfigs.Any(e => e.UId == id);
    }
}
