using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Models;

namespace PMS.Web.Controllers;

public class SetupController : Controller
{
    private readonly PMSDbContext _context;

    public SetupController(PMSDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveModule = "Setup";
        
        // Load configurations for summary
        var configurations = await _context.Configurations.ToListAsync();
        ViewBag.TotalConfigurations = configurations.Count;
        ViewBag.ActiveConfigurations = configurations.Count(c => c.IsActive);
        ViewBag.InactiveConfigurations = configurations.Count(c => !c.IsActive);
        ViewBag.Configurations = configurations;
        
        return View();
    }

    public IActionResult Roles()
    {
        ViewBag.ActiveModule = "Setup";
        return View();
    }

    public IActionResult Users()
    {
        ViewBag.ActiveModule = "Setup";
        return View();
    }

    // GET: Setup/Projects
    public async Task<IActionResult> Projects()
    {
        ViewBag.ActiveModule = "Setup";
        ViewBag.ActionPrefix = "Project";
        var projects = await _context.Projects.OrderBy(p => p.ProjectName).ToListAsync();
        return View(projects);
    }

    // GET: Setup/Projects/Details/5
    public async Task<IActionResult> ProjectDetails(int? id)
    {
        ViewBag.ActiveModule = "Setup";
        ViewBag.ActionPrefix = "Project";
        if (id == null)
        {
            return NotFound();
        }

        var project = await _context.Projects
            .FirstOrDefaultAsync(m => m.Id == id);
        if (project == null)
        {
            return NotFound();
        }

        return View(project);
    }

    // GET: Setup/Projects/Create
    public IActionResult ProjectCreate()
    {
        ViewBag.ActiveModule = "Setup";
        ViewBag.ActionPrefix = "Project";
        return View();
    }

    // POST: Setup/Projects/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProjectCreate([Bind("ProjectName,ProjectDescription,SubProject,Prefix")] Project project)
    {
        ViewBag.ActiveModule = "Setup";
        if (ModelState.IsValid)
        {
            project.CreatedAt = DateTime.Now;
            if (string.IsNullOrWhiteSpace(project.SubProject))
            {
                project.SubProject = "MAIN";
            }
            _context.Add(project);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Projects));
        }
        return View(project);
    }

    // GET: Setup/Projects/Edit/5
    public async Task<IActionResult> ProjectEdit(int? id)
    {
        ViewBag.ActiveModule = "Setup";
        ViewBag.ActionPrefix = "Project";
        if (id == null)
        {
            return NotFound();
        }

        var project = await _context.Projects.FindAsync(id);
        if (project == null)
        {
            return NotFound();
        }
        return View(project);
    }

    // POST: Setup/Projects/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProjectEdit(int id, [Bind("Id,ProjectName,ProjectDescription,CreatedAt,SubProject,Prefix")] Project project)
    {
        ViewBag.ActiveModule = "Setup";
        if (id != project.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(project.SubProject))
                {
                    project.SubProject = "MAIN";
                }
                _context.Update(project);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProjectExists(project.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Projects));
        }
        return View(project);
    }

    // GET: Setup/Projects/Delete/5
    public async Task<IActionResult> ProjectDelete(int? id)
    {
        ViewBag.ActiveModule = "Setup";
        ViewBag.ActionPrefix = "Project";
        if (id == null)
        {
            return NotFound();
        }

        var project = await _context.Projects
            .FirstOrDefaultAsync(m => m.Id == id);
        if (project == null)
        {
            return NotFound();
        }

        return View(project);
    }

    // POST: Setup/Projects/Delete/5
    [HttpPost, ActionName("ProjectDelete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProjectDeleteConfirmed(int id)
    {
        ViewBag.ActiveModule = "Setup";
        var project = await _context.Projects.FindAsync(id);
        if (project != null)
        {
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Projects));
    }

    private bool ProjectExists(int id)
    {
        return _context.Projects.Any(e => e.Id == id);
    }

    // GET: Setup/Proj
    [HttpGet]
    [Route("Setup/Proj")]
    public async Task<IActionResult> Proj()
    {
        ViewBag.ActiveModule = "Setup";
        ViewBag.ActionPrefix = "Proj";
        var projects = await _context.Projects.OrderBy(p => p.ProjectName).ToListAsync();
        return View("Projects", projects);
    }

    // GET: Setup/Proj/Details/5
    [HttpGet("Setup/Proj/Details/{id}")]
    public async Task<IActionResult> ProjDetails(int? id)
    {
        ViewBag.ActiveModule = "Setup";
        ViewBag.ActionPrefix = "Proj";
        if (id == null)
        {
            return NotFound();
        }

        var project = await _context.Projects
            .FirstOrDefaultAsync(m => m.Id == id);
        if (project == null)
        {
            return NotFound();
        }

        return View("ProjectDetails", project);
    }

    // GET: Setup/Proj/Create
    [HttpGet("Setup/Proj/Create")]
    public IActionResult ProjCreate()
    {
        ViewBag.ActiveModule = "Setup";
        ViewBag.ActionPrefix = "Proj";
        return View("ProjectCreate");
    }

    // POST: Setup/Proj/Create
    [HttpPost("Setup/Proj/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProjCreate([Bind("ProjectName,ProjectDescription,SubProject,Prefix")] Project project)
    {
        ViewBag.ActiveModule = "Setup";
        if (ModelState.IsValid)
        {
            project.CreatedAt = DateTime.Now;
            if (string.IsNullOrWhiteSpace(project.SubProject))
            {
                project.SubProject = "MAIN";
            }
            _context.Add(project);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Proj));
        }
        return View("ProjectCreate", project);
    }

    // GET: Setup/Proj/Edit/5
    [HttpGet("Setup/Proj/Edit/{id}")]
    public async Task<IActionResult> ProjEdit(int? id)
    {
        ViewBag.ActiveModule = "Setup";
        ViewBag.ActionPrefix = "Proj";
        if (id == null)
        {
            return NotFound();
        }

        var project = await _context.Projects.FindAsync(id);
        if (project == null)
        {
            return NotFound();
        }
        return View("ProjectEdit", project);
    }

    // POST: Setup/Proj/Edit/5
    [HttpPost("Setup/Proj/Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProjEdit(int id, [Bind("Id,ProjectName,ProjectDescription,CreatedAt,SubProject,Prefix")] Project project)
    {
        ViewBag.ActiveModule = "Setup";
        if (id != project.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(project.SubProject))
                {
                    project.SubProject = "MAIN";
                }
                _context.Update(project);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProjectExists(project.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Proj));
        }
        return View("ProjectEdit", project);
    }

    // GET: Setup/Proj/Delete/5
    [HttpGet("Setup/Proj/Delete/{id}")]
    public async Task<IActionResult> ProjDelete(int? id)
    {
        ViewBag.ActiveModule = "Setup";
        ViewBag.ActionPrefix = "Proj";
        if (id == null)
        {
            return NotFound();
        }

        var project = await _context.Projects
            .FirstOrDefaultAsync(m => m.Id == id);
        if (project == null)
        {
            return NotFound();
        }

        return View("ProjectDelete", project);
    }

    // POST: Setup/Proj/Delete/5
    [HttpPost("Setup/Proj/Delete/{id}"), ActionName("ProjDelete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProjDeleteConfirmed(int id)
    {
        ViewBag.ActiveModule = "Setup";
        var project = await _context.Projects.FindAsync(id);
        if (project != null)
        {
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Proj));
    }
}

