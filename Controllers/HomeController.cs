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
        ViewBag.TotalPlots = 0; // Placeholder - will be replaced when Plots model is added
        ViewBag.TotalPaymentsCollected = 0m; // Placeholder - will be replaced when Payments model is added
        ViewBag.PendingPayments = 0; // Placeholder - will be replaced when Payments model is added
        ViewBag.ActiveAllotments = 0; // Placeholder - will be replaced when Allotments model is added
        ViewBag.TransfersThisMonth = 0; // Placeholder - will be replaced when Transfers model is added

        // Recent Activity
        var recentCustomers = await _context.Customers
            .OrderByDescending(c => c.CreationDate)
            .Take(5)
            .Select(c => new RecentCustomerData { CustomerNo = c.CustomerNo, FullName = c.FullName, CreationDate = c.CreationDate })
            .ToListAsync();
        ViewBag.RecentCustomers = recentCustomers;

        // Financial Snapshot (Placeholders - will be replaced when Payments model is added)
        ViewBag.TodaysCollection = 0m;
        ViewBag.ThisMonthCollection = 0m;
        ViewBag.OutstandingBalance = 0m;

        // Monthly Payments Trend (Last 6 months - Placeholder data structure)
        var monthlyTrend = new List<MonthlyTrendData>();
        for (int i = 5; i >= 0; i--)
        {
            var monthStart = thisMonthStart.AddMonths(-i);
            var monthName = monthStart.ToString("MMM yyyy");
            monthlyTrend.Add(new MonthlyTrendData { Month = monthName, Amount = 0m });
        }
        ViewBag.MonthlyPaymentsTrend = monthlyTrend;

        // Sold vs Available Plots (Placeholder)
        ViewBag.SoldPlots = 0;
        ViewBag.AvailablePlots = 0;

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
