using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Models;

namespace PMS.Web.Controllers;

// Data transfer objects for dashboard
public class MonthlyTrendData
{
    public string Month { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class AlertData
{
    public string Type { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Link { get; set; } = string.Empty;
}

public class RecentCustomerData
{
    public string CustomerNo { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
}

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly PMSDbContext _context;

    public HomeController(ILogger<HomeController> logger, PMSDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveModule = "Home";

        var today = DateTime.Today;
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var lastMonthEnd = thisMonthStart;

        // KPI Cards Data
        ViewBag.TotalCustomers = await _context.Customers.CountAsync();
        ViewBag.TotalPlots = await _context.InventoryDetails.CountAsync();

        var payments = await _context.Payments.AsNoTracking().ToListAsync();
        ViewBag.TotalPaymentsCollected = payments.Sum(p => ParseAmount(p.PaidAmount));
        ViewBag.PendingPayments = payments.Count(p => string.IsNullOrWhiteSpace(p.PaidDate));
        ViewBag.ActiveAllotments = await _context.InventoryDetails.CountAsync(p =>
            p.AllotmentStatus != null &&
            p.AllotmentStatus.Trim().ToLower() == "allotted");
        ViewBag.TransfersThisMonth = 0; // Placeholder - will be replaced when Transfers model is added

        // Recent Activity
        var recentCustomers = await _context.Customers
            .OrderByDescending(c => c.CreationDate)
            .Take(5)
            .Select(c => new RecentCustomerData { CustomerNo = c.CustomerNo, FullName = c.FullName, CreationDate = c.CreationDate })
            .ToListAsync();
        ViewBag.RecentCustomers = recentCustomers;

        // Financial Snapshot
        ViewBag.TodaysCollection = payments
            .Where(p => DateTime.TryParse(p.PaidDate, out var dt) && dt.Date == today)
            .Sum(p => ParseAmount(p.PaidAmount));
        ViewBag.ThisMonthCollection = payments
            .Where(p => DateTime.TryParse(p.PaidDate, out var dt) && dt >= thisMonthStart && dt < thisMonthStart.AddMonths(1))
            .Sum(p => ParseAmount(p.PaidAmount));
        ViewBag.OutstandingBalance = 0m;

        // Monthly Payments Trend (Last 6 months - Placeholder data structure)
        var monthlyTrend = new List<object>();
        for (int i = 5; i >= 0; i--)
        {
            var monthStart = thisMonthStart.AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            var monthName = monthStart.ToString("MMM yyyy");
            var monthTotal = payments
                .Where(p => DateTime.TryParse(p.PaidDate ?? p.CreatedOn, out var dt) && dt >= monthStart && dt < monthEnd)
                .Sum(p => ParseAmount(p.PaidAmount));
            monthlyTrend.Add(new { month = monthName, amount = monthTotal });
        }
        ViewBag.MonthlyPaymentsTrend = monthlyTrend;

        // Sold vs Available Plots
        ViewBag.SoldPlots = await _context.InventoryDetails.CountAsync(p =>
            p.AllotmentStatus != null &&
            p.AllotmentStatus.Trim().ToLower() == "allotted");
        ViewBag.AvailablePlots = await _context.InventoryDetails.CountAsync(p =>
            p.AllotmentStatus != null &&
            p.AllotmentStatus.Trim().ToLower() == "available");

        // Alerts & Notifications (Placeholders - will be replaced when respective models are added)
        var alerts = new List<AlertData>();
        // Overdue payments alert
        alerts.Add(new AlertData { Type = "warning", Icon = "⚠️", Message = "5 payments overdue", Count = 5, Link = "/Payments" });
        // Pending transfers alert
        alerts.Add(new AlertData { Type = "info", Icon = "📋", Message = "3 transfer requests pending", Count = 3, Link = "/Transfers" });
        // Upcoming schedules alert
        alerts.Add(new AlertData { Type = "success", Icon = "📅", Message = "10 payment schedules due this week", Count = 10, Link = "/Schedules" });
        ViewBag.Alerts = alerts;

        return View();
    }

    private static decimal ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        return decimal.TryParse(value, out var amount) ? amount : 0m;
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
