using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Models;
using PMS.Web.ViewModels;

namespace PMS.Web.Controllers;

public class DealersController : Controller
{
    private readonly PMSDbContext _context;

    public DealersController(PMSDbContext context)
    {
        _context = context;
    }

    private async Task<DealerPerformanceViewModel> BuildDealerPerformanceModelAsync()
    {
        var dealers = await _context.Dealers.OrderBy(d => d.DealershipName).ToListAsync();
        var customers = await _context.Customers.Where(c => c.DealerID != null).Include(c => c.Dealer).ToListAsync();
        var payments = await _context.Payments.ToListAsync();

        var dealerStats = new List<DealerPerformanceItem>();
        foreach (var dealer in dealers)
        {
            var dealerCustomers = customers.Where(c => c.DealerID == dealer.DealerID).ToList();
            var customerNos = dealerCustomers.Select(c => c.CustomerNo).Where(x => x != null).ToHashSet();
            var dealerPayments = payments.Where(p => p.customerno != null && customerNos.Contains(p.customerno)).ToList();
            var totalRevenue = dealerPayments.Sum(p => ParseAmount(p.PaidAmount));

            var planUsage = dealerCustomers
                .Where(c => !string.IsNullOrWhiteSpace(c.PlanNo))
                .GroupBy(c => c.PlanNo!)
                .Select(g => new PlanUsageItem
                {
                    PlanNo = g.Key,
                    CustomerCount = g.Count(),
                    Revenue = payments
                        .Where(p => p.customerno != null && g.Select(x => x.CustomerNo).Contains(p.customerno))
                        .Sum(p => ParseAmount(p.PaidAmount))
                })
                .OrderByDescending(x => x.CustomerCount)
                .ToList();

            dealerStats.Add(new DealerPerformanceItem
            {
                DealerID = dealer.DealerID,
                DealershipName = dealer.DealershipName ?? "",
                Status = dealer.Status ?? "",
                CustomersAcquired = dealerCustomers.Count,
                TotalRevenue = totalRevenue,
                PlanUsage = planUsage,
                PaymentCount = dealerPayments.Count
            });
        }

        var totalCustomersAcquired = dealerStats.Sum(d => d.CustomersAcquired);
        var totalRevenueFromDealers = dealerStats.Sum(d => d.TotalRevenue);
        var dealersWithCustomers = dealerStats.Count(d => d.CustomersAcquired > 0);
        var activeDealers = dealers.Count(d => string.Equals(d.Status, "Active", StringComparison.OrdinalIgnoreCase));

        return new DealerPerformanceViewModel
        {
            KPIs = new DealerPerformanceKPIs
            {
                TotalDealers = dealers.Count,
                ActiveDealers = activeDealers,
                TotalCustomersAcquired = totalCustomersAcquired,
                TotalRevenueFromDealers = totalRevenueFromDealers,
                AverageRevenuePerDealer = dealersWithCustomers > 0 ? totalRevenueFromDealers / dealersWithCustomers : 0,
                DealersWithCustomers = dealersWithCustomers
            },
            DealerStats = dealerStats.OrderByDescending(d => d.TotalRevenue).ToList()
        };
    }

    // GET: Dealers (main page - same content as Dashboard)
    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveModule = "Dealers";
        var model = await BuildDealerPerformanceModelAsync();
        return View("Dashboard", model);
    }

    // GET: Dealers/Dashboard - Dealer Performance Report (same as Index)
    public async Task<IActionResult> Dashboard()
    {
        ViewBag.ActiveModule = "Dealers";
        var model = await BuildDealerPerformanceModelAsync();
        return View(model);
    }

    private static decimal ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0m;
        return decimal.TryParse(value.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amount) ? amount : 0m;
    }

    // GET: Dealers/AllDealers
    public async Task<IActionResult> AllDealers()
    {
        ViewBag.ActiveModule = "Dealers";
        var dealers = await _context.Dealers.OrderBy(d => d.DealershipName).ToListAsync();
        return View("Index", dealers);
    }

    // GET: Dealers/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        ViewBag.ActiveModule = "Dealers";
        if (id == null)
            return NotFound();

        var dealer = await _context.Dealers.FirstOrDefaultAsync(m => m.DealerID == id);
        if (dealer == null)
            return NotFound();

        return View(dealer);
    }

    // GET: Dealers/Create
    public IActionResult Create()
    {
        ViewBag.ActiveModule = "Dealers";
        return View(new Dealer { Status = "Active", RegisterationDate = DateTime.Today, MembershipType = "Standard", IncentivePercentage = 5.0 });
    }

    // POST: Dealers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("DealershipName,RegisterationDate,Status,MembershipType,OwnerName,OwnerCNIC,MobileNo,PhoneNumber,Email,Address,OwnerDetails,Details,IncentivePercentage")] Dealer dealer)
    {
        ViewBag.ActiveModule = "Dealers";
        if (ModelState.IsValid)
        {
            _context.Dealers.Add(dealer);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Dealer created successfully.";
            return RedirectToAction(nameof(Index));
        }
        return View(dealer);
    }

    // GET: Dealers/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        ViewBag.ActiveModule = "Dealers";
        if (id == null)
            return NotFound();

        var dealer = await _context.Dealers.FindAsync(id);
        if (dealer == null)
            return NotFound();

        return View(dealer);
    }

    // POST: Dealers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("DealerID,DealershipName,RegisterationDate,Status,MembershipType,OwnerName,OwnerCNIC,MobileNo,PhoneNumber,Email,Address,OwnerDetails,Details,IncentivePercentage")] Dealer dealer)
    {
        ViewBag.ActiveModule = "Dealers";
        if (id != dealer.DealerID)
            return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(dealer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Dealer updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DealerExists(dealer.DealerID))
                    return NotFound();
                throw;
            }
        }
        return View(dealer);
    }

    // GET: Dealers/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        ViewBag.ActiveModule = "Dealers";
        if (id == null)
            return NotFound();

        var dealer = await _context.Dealers.FirstOrDefaultAsync(m => m.DealerID == id);
        if (dealer == null)
            return NotFound();

        return View(dealer);
    }

    // POST: Dealers/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        ViewBag.ActiveModule = "Dealers";
        var dealer = await _context.Dealers.FindAsync(id);
        if (dealer != null)
        {
            _context.Dealers.Remove(dealer);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Dealer deleted successfully.";
        }
        return RedirectToAction(nameof(Index));
    }

    private bool DealerExists(int id)
    {
        return _context.Dealers.Any(e => e.DealerID == id);
    }
}
