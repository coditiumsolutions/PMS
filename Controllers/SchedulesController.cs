using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Models;
using PMS.Web.ViewModels;
using System.Text.Json;

namespace PMS.Web.Controllers;

public class SchedulesController : Controller
{
    private readonly PMSDbContext _context;

    public SchedulesController(PMSDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveModule = "Schedules";
        await LoadDashboardData();
        return View("Dashboard");
    }

    public async Task<IActionResult> Dashboard()
    {
        ViewBag.ActiveModule = "Schedules";
        await LoadDashboardData();
        return View();
    }

    public IActionResult Details()
    {
        ViewBag.ActiveModule = "Schedules";
        return View();
    }

    public IActionResult Operations()
    {
        ViewBag.ActiveModule = "Schedules";
        return View();
    }

    public async Task<IActionResult> Summary()
    {
        ViewBag.ActiveModule = "Schedules";

        var plans = await _context.PaymentPlans.AsNoTracking().ToListAsync();
        var children = await _context.PaymentPlanChildren.AsNoTracking().ToListAsync();

        ViewBag.TotalPlans = plans.Count;
        ViewBag.TotalChildPayments = children.Count;
        ViewBag.TotalPlanAmount = plans.Sum(p => ParseAmount(p.totalamount));
        ViewBag.TotalDueAmount = children.Sum(c => ParseAmount(c.dueamount));

        ViewBag.PlanTypeGroups = plans
            .GroupBy(p => string.IsNullOrWhiteSpace(p.paymenttype) ? "Unknown" : p.paymenttype.Trim())
            .OrderByDescending(g => g.Count())
            .Select(g => new { Type = g.Key, Count = g.Count(), Amount = g.Sum(x => ParseAmount(x.totalamount)) })
            .ToList();

        ViewBag.ChildPlanGroups = children
            .GroupBy(c => string.IsNullOrWhiteSpace(c.planno) ? "Unknown" : c.planno.Trim())
            .OrderByDescending(g => g.Count())
            .Select(g => new { PlanNo = g.Key, Count = g.Count(), Amount = g.Sum(x => ParseAmount(x.dueamount)) })
            .Take(10)
            .ToList();

        return View();
    }

    public IActionResult History()
    {
        ViewBag.ActiveModule = "Schedules";
        return View();
    }

    public async Task<IActionResult> AllPlans()
    {
        ViewBag.ActiveModule = "Schedules";
        var plans = await _context.PaymentPlans.AsNoTracking()
            .OrderByDescending(p => p.uid)
            .ToListAsync();
        return View(plans);
    }

    public async Task<IActionResult> AllPayments()
    {
        ViewBag.ActiveModule = "Schedules";
        var payments = await _context.PaymentPlanChildren.AsNoTracking()
            .OrderByDescending(p => p.uid)
            .ToListAsync();
        return View(payments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddChildInline(PaymentPlanChildInlineCreateViewModel model)
    {
        if (!Request.Headers.ContainsKey("X-Requested-With"))
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { errors });
        }

        var child = new PaymentPlanChild
        {
            planno = model.Planno,
            paymentdesc = model.PaymentDesc,
            installmentno = model.InstallmentNo,
            duedate = model.DueDate,
            dueamount = model.DueAmount
        };

        _context.PaymentPlanChildren.Add(child);
        await _context.SaveChangesAsync();

        var children = await _context.PaymentPlanChildren.AsNoTracking()
            .Where(c => c.planno == model.Planno)
            .OrderBy(c => c.installmentno)
            .ToListAsync();

        return PartialView("_ChildPaymentsTable", children);
    }

    /// <summary>
    /// Shows a selected payment plan together with its child payment schedule.
    /// </summary>
    public async Task<IActionResult> PlanSchedule(string planno)
    {
        ViewBag.ActiveModule = "Schedules";

        if (string.IsNullOrWhiteSpace(planno))
        {
            return RedirectToAction(nameof(AllPlans));
        }

        var plan = await _context.PaymentPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.planno == planno);

        if (plan == null)
        {
            return NotFound();
        }

        var children = await _context.PaymentPlanChildren.AsNoTracking()
            .Where(c => c.planno == planno)
            .OrderBy(c => c.installmentno)
            .ToListAsync();

        var viewModel = new PaymentPlanScheduleViewModel
        {
            Plan = plan,
            Children = children
        };

        // Populate payment description options for inline child form
        ViewBag.PaymentDescriptions = await _context.Configurations
            .AsNoTracking()
            .Where(c => c.ConfigKey == "PaymentDesc")
            .Select(c => c.ConfigValue)
            .ToListAsync();

        return View(viewModel);
    }

    public async Task<IActionResult> PlanDetails(int? id)
    {
        ViewBag.ActiveModule = "Schedules";
        if (id == null)
        {
            return NotFound();
        }

        var plan = await _context.PaymentPlans.FirstOrDefaultAsync(p => p.uid == id);
        if (plan == null)
        {
            return NotFound();
        }

        return View(plan);
    }

    public IActionResult CreatePlan()
    {
        ViewBag.ActiveModule = "Schedules";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePlan([Bind("planno,plandetail,totalamount,paymenttype,comments,createdby,creationdate,sizedetail")] PaymentPlan paymentPlan)
    {
        ViewBag.ActiveModule = "Schedules";
        if (ModelState.IsValid)
        {
            _context.Add(paymentPlan);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AllPlans));
        }

        return View(paymentPlan);
    }

    public async Task<IActionResult> EditPlan(int? id)
    {
        ViewBag.ActiveModule = "Schedules";
        if (id == null)
        {
            return NotFound();
        }

        var paymentPlan = await _context.PaymentPlans.FindAsync(id);
        if (paymentPlan == null)
        {
            return NotFound();
        }

        return View(paymentPlan);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPlan(int id, [Bind("uid,planno,plandetail,totalamount,paymenttype,comments,createdby,creationdate,sizedetail")] PaymentPlan paymentPlan)
    {
        ViewBag.ActiveModule = "Schedules";
        if (id != paymentPlan.uid)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(paymentPlan);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PaymentPlanExists(paymentPlan.uid))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(AllPlans));
        }

        return View(paymentPlan);
    }

    public async Task<IActionResult> DeletePlan(int? id)
    {
        ViewBag.ActiveModule = "Schedules";
        if (id == null)
        {
            return NotFound();
        }

        var paymentPlan = await _context.PaymentPlans.FirstOrDefaultAsync(p => p.uid == id);
        if (paymentPlan == null)
        {
            return NotFound();
        }

        return View(paymentPlan);
    }

    [HttpPost, ActionName("DeletePlan")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePlanConfirmed(int id)
    {
        ViewBag.ActiveModule = "Schedules";
        var paymentPlan = await _context.PaymentPlans.FindAsync(id);
        if (paymentPlan != null)
        {
            _context.PaymentPlans.Remove(paymentPlan);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(AllPlans));
    }

    public async Task<IActionResult> ChildDetails(int? id)
    {
        ViewBag.ActiveModule = "Schedules";
        if (id == null)
        {
            return NotFound();
        }

        var child = await _context.PaymentPlanChildren.FirstOrDefaultAsync(p => p.uid == id);
        if (child == null)
        {
            return NotFound();
        }

        return View(child);
    }

    public IActionResult CreateChild(string? planno)
    {
        ViewBag.ActiveModule = "Schedules";
        var model = new PaymentPlanChild();
        if (!string.IsNullOrWhiteSpace(planno))
        {
            model.planno = planno;
        }
        // Set default creation date to today
        model.creationdate = DateTime.Today.ToString("yyyy-MM-dd");
        // Populate dropdowns
        ViewBag.PaymentDescriptions = _context.Configurations
            .AsNoTracking()
            .Where(c => c.ConfigKey == "PaymentDesc")
            .Select(c => c.ConfigValue)
            .ToList();

        ViewBag.SurchargePolicies = new List<string> { "Active", "Not Active" };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateChild([Bind("planno,paymentdesc,installmentno,duedate,dueamount,surchargepolicy,surchargerate,discount,comments,createdby,creationdate")] PaymentPlanChild paymentPlanChild)
    {
        ViewBag.ActiveModule = "Schedules";
        // Repopulate dropdowns for redisplay
        ViewBag.PaymentDescriptions = await _context.Configurations
            .AsNoTracking()
            .Where(c => c.ConfigKey == "PaymentDesc")
            .Select(c => c.ConfigValue)
            .ToListAsync();
        ViewBag.SurchargePolicies = new List<string> { "Active", "Not Active" };
        
        if (!ModelState.IsValid)
        {
            return View(paymentPlanChild);
        }

        try
        {
            _context.Add(paymentPlanChild);
            await _context.SaveChangesAsync();
            if (!string.IsNullOrWhiteSpace(paymentPlanChild.planno))
            {
                return RedirectToAction(nameof(PlanSchedule), new { planno = paymentPlanChild.planno });
            }

            return RedirectToAction(nameof(AllPayments));
        }
        catch (DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            ModelState.AddModelError(string.Empty, $"Database error while saving child payment: {message}");
            return View(paymentPlanChild);
        }
    }

    public async Task<IActionResult> EditChild(int? id)
    {
        ViewBag.ActiveModule = "Schedules";
        if (id == null)
        {
            return NotFound();
        }

        var paymentPlanChild = await _context.PaymentPlanChildren.FindAsync(id);
        if (paymentPlanChild == null)
        {
            return NotFound();
        }

        return View(paymentPlanChild);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditChild(int id, [Bind("uid,planno,paymentdesc,installmentno,duedate,dueamount,surchargepolicy,surchargerate,discount,comments,createdby,creationdate")] PaymentPlanChild paymentPlanChild)
    {
        ViewBag.ActiveModule = "Schedules";
        if (id != paymentPlanChild.uid)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(paymentPlanChild);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PaymentPlanChildExists(paymentPlanChild.uid))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(AllPayments));
        }

        return View(paymentPlanChild);
    }

    public async Task<IActionResult> DeleteChild(int? id)
    {
        ViewBag.ActiveModule = "Schedules";
        if (id == null)
        {
            return NotFound();
        }

        var paymentPlanChild = await _context.PaymentPlanChildren.FirstOrDefaultAsync(p => p.uid == id);
        if (paymentPlanChild == null)
        {
            return NotFound();
        }

        return View(paymentPlanChild);
    }

    [HttpPost, ActionName("DeleteChild")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteChildConfirmed(int id)
    {
        ViewBag.ActiveModule = "Schedules";
        var paymentPlanChild = await _context.PaymentPlanChildren.FindAsync(id);
        if (paymentPlanChild != null)
        {
            _context.PaymentPlanChildren.Remove(paymentPlanChild);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(AllPayments));
    }

    private bool PaymentPlanExists(int id)
    {
        return _context.PaymentPlans.Any(e => e.uid == id);
    }

    private bool PaymentPlanChildExists(int id)
    {
        return _context.PaymentPlanChildren.Any(e => e.uid == id);
    }

    private async Task LoadDashboardData()
    {
        var plans = await _context.PaymentPlans.AsNoTracking().ToListAsync();
        var children = await _context.PaymentPlanChildren.AsNoTracking().ToListAsync();

        var planTypeGroups = plans
            .GroupBy(p => string.IsNullOrWhiteSpace(p.paymenttype) ? "Unknown" : p.paymenttype.Trim())
            .OrderByDescending(g => g.Count())
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .ToList();

        var childPlanGroups = children
            .GroupBy(c => string.IsNullOrWhiteSpace(c.planno) ? "Unknown" : c.planno.Trim())
            .OrderByDescending(g => g.Count())
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .Take(10)
            .ToList();

        ViewBag.PlanTypeLabels = JsonSerializer.Serialize(planTypeGroups.Select(p => p.Label));
        ViewBag.PlanTypeCounts = JsonSerializer.Serialize(planTypeGroups.Select(p => p.Count));
        ViewBag.ChildPlanLabels = JsonSerializer.Serialize(childPlanGroups.Select(p => p.Label));
        ViewBag.ChildPlanCounts = JsonSerializer.Serialize(childPlanGroups.Select(p => p.Count));
    }

    private static decimal ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        return decimal.TryParse(value, out var amount) ? amount : 0m;
    }
}

