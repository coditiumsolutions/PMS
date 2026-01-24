using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Models;

namespace PMS.Web.Controllers;

public class PaymentsController : Controller
{
    private readonly PMSDbContext _context;

    public PaymentsController(PMSDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveModule = "Payments";
        await LoadDashboardData();
        return View("Dashboard");
    }

    public async Task<IActionResult> Dashboard()
    {
        ViewBag.ActiveModule = "Payments";
        await LoadDashboardData();
        return View();
    }

    private async Task LoadDashboardData()
    {
        var payments = await _context.Payments.AsNoTracking().ToListAsync();

        var methodGroups = payments
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Method) ? "Unknown" : p.Method.Trim())
            .OrderBy(g => g.Key)
            .Select(g => new { Method = g.Key, Count = g.Count() })
            .ToList();

        var monthlyTotals = payments
            .Select(p =>
            {
                var dateText = p.PaidDate ?? p.CreatedOn;
                return DateTime.TryParse(dateText, out var dt)
                    ? new { Month = dt.ToString("yyyy-MM"), Amount = ParseAmount(p.PaidAmount) }
                    : null;
            })
            .Where(x => x != null)
            .GroupBy(x => x!.Month)
            .OrderBy(g => g.Key)
            .Select(g => new { Month = g.Key, Total = g.Sum(v => v!.Amount) })
            .ToList();

        ViewBag.MethodLabels = System.Text.Json.JsonSerializer.Serialize(methodGroups.Select(m => m.Method));
        ViewBag.MethodCounts = System.Text.Json.JsonSerializer.Serialize(methodGroups.Select(m => m.Count));
        ViewBag.MonthLabels = System.Text.Json.JsonSerializer.Serialize(monthlyTotals.Select(m => m.Month));
        ViewBag.MonthTotals = System.Text.Json.JsonSerializer.Serialize(monthlyTotals.Select(m => m.Total));
    }

    private static decimal ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        return decimal.TryParse(value, out var amount) ? amount : 0m;
    }

    public IActionResult Details()
    {
        ViewBag.ActiveModule = "Payments";
        return View();
    }

    public IActionResult Operations()
    {
        ViewBag.ActiveModule = "Payments";
        return View();
    }

    public IActionResult Summary()
    {
        ViewBag.ActiveModule = "Payments";
        return View();
    }

    public IActionResult History()
    {
        ViewBag.ActiveModule = "Payments";
        return View();
    }

    // GET: Payments/AllPayments
    public async Task<IActionResult> AllPayments(string searchCustomer = "", int page = 1, int pageSize = 10)
    {
        ViewBag.ActiveModule = "Payments";
        
        IQueryable<Payment> paymentsQuery = _context.Payments.OrderByDescending(p => p.uId);
        
        if (!string.IsNullOrWhiteSpace(searchCustomer))
        {
            var searchValue = searchCustomer.Trim().ToLower();
            paymentsQuery = paymentsQuery.Where(p =>
                p.customerno != null && p.customerno.ToLower().Contains(searchValue));
        }
        
        var totalRecords = await paymentsQuery.CountAsync();
        var paginatedPayments = await paymentsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        
        ViewBag.SearchCustomer = searchCustomer;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        ViewBag.TotalRecords = totalRecords;
        ViewBag.PageSize = pageSize;
        
        return View(paginatedPayments);
    }

    public IActionResult Verified()
    {
        ViewBag.ActiveModule = "Payments";
        return View();
    }

    public IActionResult UnVerified()
    {
        ViewBag.ActiveModule = "Payments";
        return View();
    }

    // GET: Payments/AllChallans
    public async Task<IActionResult> AllChallans(string paymentStatus = "", string searchRecord = "", int page = 1, int pageSize = 10)
    {
        ViewBag.ActiveModule = "Payments";
        
        var allChallans = await _context.Challans.OrderByDescending(c => c.uid).ToListAsync();
        
        // Filter by payment status if provided
        if (!string.IsNullOrWhiteSpace(paymentStatus))
        {
            allChallans = allChallans.Where(c => 
                c.bankverified != null && 
                c.bankverified.Trim().Equals(paymentStatus, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }
        
        // Filter by search record (depositslipno or customerno)
        if (!string.IsNullOrWhiteSpace(searchRecord))
        {
            var searchTerm = searchRecord.Trim().ToLower();
            allChallans = allChallans.Where(c =>
                (!string.IsNullOrEmpty(c.depsoiteslipno) && c.depsoiteslipno.ToLower().Contains(searchTerm)) ||
                (!string.IsNullOrEmpty(c.customerno) && c.customerno.ToLower().Contains(searchTerm))
            ).ToList();
        }
        
        // Pagination
        var totalRecords = allChallans.Count;
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        var paginatedChallans = allChallans.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        
        ViewBag.PaymentStatus = paymentStatus;
        ViewBag.SearchRecord = searchRecord;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalRecords = totalRecords;
        ViewBag.PageSize = pageSize;
        
        return View(paginatedChallans);
    }

    // GET: Payments/PendingChall
    public async Task<IActionResult> PendingChall()
    {
        ViewBag.ActiveModule = "Payments";
        var allChallans = await _context.Challans.ToListAsync();
        var pendingChallans = allChallans
            .Where(c => c.bankverified != null && 
                       c.bankverified.Trim().Equals("Pending", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.uid)
            .ToList();
        return View(pendingChallans);
    }

    // GET: Payments/UnVerifiedChall
    public async Task<IActionResult> UnVerifiedChall()
    {
        ViewBag.ActiveModule = "Payments";
        var allChallans = await _context.Challans.ToListAsync();
        var unVerifiedChallans = allChallans
            .Where(c => c.bankverified != null && 
                       (c.bankverified.Trim().Equals("Un-Verified", StringComparison.OrdinalIgnoreCase) ||
                        c.bankverified.Trim().Equals("Unverified", StringComparison.OrdinalIgnoreCase) ||
                        c.bankverified.Trim().Equals("Un-Verified Chall", StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(c => c.uid)
            .ToList();
        return View(unVerifiedChallans);
    }
}

