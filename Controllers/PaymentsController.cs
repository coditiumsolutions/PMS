using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using PMS.Web.Data;
using PMS.Web.Models;
using PMS.Web.ViewModels;

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

    // GET: Payments/ChallanDetails/5
    public async Task<IActionResult> ChallanDetails(int id)
    {
        ViewBag.ActiveModule = "Payments";

        var challan = await _context.Challans
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.uid == id);

        if (challan == null)
        {
            return NotFound();
        }

        return View(challan);
    }

    // GET: Payments/CreateChallan
    public async Task<IActionResult> CreateChallan(string? customerno = null)
    {
        ViewBag.ActiveModule = "Payments";

        var challan = new Challan
        {
            customerno = customerno,
            creationdate = DateTime.Today,
            currency = "Rs"
        };

        // Dropdown sources
        ViewBag.BankNames = await _context.Configurations
            .AsNoTracking()
            .Where(c => c.ConfigKey == "Bank")
            .Select(c => c.ConfigValue)
            .ToListAsync();

        ViewBag.BankVerifiedStatuses = await _context.Configurations
            .AsNoTracking()
            .Where(c => c.ConfigKey == "Bank")
            .Select(c => c.ConfigValue)
            .ToListAsync();

        ViewBag.DepositTypes = new List<string> { "Cash", "DD", "Cheque" };
        ViewBag.Currencies = new List<string> { "Rs", "Dollar" };

        return View(challan);
    }

    // POST: Payments/CreateChallan
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateChallan(Challan challan)
    {
        ViewBag.ActiveModule = "Payments";

        // Local helper to repopulate dropdowns whenever we need to redisplay the form
        async Task PopulateDropdowns()
        {
            ViewBag.BankNames = await _context.Configurations
                .AsNoTracking()
                .Where(c => c.ConfigKey == "Bank")
                .Select(c => c.ConfigValue)
                .ToListAsync();

            ViewBag.BankVerifiedStatuses = await _context.Configurations
                .AsNoTracking()
                .Where(c => c.ConfigKey == "Bank")
                .Select(c => c.ConfigValue)
                .ToListAsync();

            ViewBag.DepositTypes = new List<string> { "Cash", "DD", "Cheque" };
            ViewBag.Currencies = new List<string> { "Rs", "Dollar" };
        }

        // Basic model validation
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns();
            return View(challan);
        }

        // Server-side check for existing challan with same customer and bank
        if (!string.IsNullOrWhiteSpace(challan.customerno) &&
            !string.IsNullOrWhiteSpace(challan.bankname))
        {
            var duplicateExists = await _context.Challans
                .AsNoTracking()
                .AnyAsync(c =>
                    c.customerno == challan.customerno &&
                    c.bankname == challan.bankname);

            if (duplicateExists)
            {
                ModelState.AddModelError(string.Empty,
                    "A challan already exists for this customer and bank. Please use a different bank or update the existing challan.");

                await PopulateDropdowns();
                return View(challan);
            }
        }

        if (!challan.creationdate.HasValue)
        {
            challan.creationdate = DateTime.Today;
        }

        _context.Challans.Add(challan);

        try
        {
            await _context.SaveChangesAsync();
            TempData["ChallanMessage"] = "Challan saved successfully.";
            TempData["ChallanMessageType"] = "success";
            return RedirectToAction(nameof(AllChallans));
        }
        catch (DbUpdateException ex)
        {
            var sqlEx = ex.InnerException as SqlException;
            if (sqlEx != null && (sqlEx.Number == 2627 || sqlEx.Number == 2601) &&
                sqlEx.Message.Contains("UQ_Deposit_Bank"))
            {
                ModelState.AddModelError(string.Empty,
                    "A challan with this customer and bank already exists. Please check existing challans or choose a different bank.");
            }
            else
            {
                ModelState.AddModelError(string.Empty,
                    "The record could not be saved due to a database error. Please try again or contact support.");
            }

            await PopulateDropdowns();
            return View(challan);
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty,
                "An unexpected error occurred while saving. Please try again.");

            await PopulateDropdowns();
            return View(challan);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateChallanStatus(int id, string bankverified)
    {
        ViewBag.ActiveModule = "Payments";

        var challan = await _context.Challans.FirstOrDefaultAsync(c => c.uid == id);
        if (challan == null)
        {
            return NotFound();
        }

        challan.bankverified = bankverified;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(ChallanDetails), new { id });
    }

    // GET: Payments/ProcessChallanPayments/5
    public async Task<IActionResult> ProcessChallanPayments(int id)
    {
        ViewBag.ActiveModule = "Payments";

        var challan = await _context.Challans
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.uid == id);

        if (challan == null)
        {
            return NotFound();
        }

        var payments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.customerno == challan.customerno)
            .OrderByDescending(p => p.uId)
            .ToListAsync();

        var viewModel = new ChallanPaymentsViewModel
        {
            Challan = challan,
            Payments = payments,
            NewPayment = new Payment
            {
                customerno = challan.customerno,
                BankName = challan.bankname ?? string.Empty,
                DSNo = challan.depsoiteslipno,
                DSDate = challan.depositdate?.ToString("yyyy-MM-dd")
            }
        };

        // Populate dropdown sources
        ViewBag.BankNames = await _context.Configurations
            .AsNoTracking()
            .Where(c => c.ConfigKey == "Bank")
            .Select(c => c.ConfigValue)
            .ToListAsync();

        ViewBag.PaymentDescriptions = await _context.Configurations
            .AsNoTracking()
            .Where(c => c.ConfigKey == "PaymentDesc")
            .Select(c => c.ConfigValue)
            .ToListAsync();

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPaymentInline(int challanId, [Bind(Prefix = "NewPayment")] Payment input)
    {
        if (!Request.Headers.ContainsKey("X-Requested-With"))
        {
            return BadRequest();
        }

        var challan = await _context.Challans
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.uid == challanId);

        if (challan == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(input.PaidAmount))
        {
            return BadRequest(new { errors = new[] { "Paid Amount is required." } });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new { errors });
        }

        var payment = new Payment
        {
            customerno = challan.customerno ?? string.Empty,
            PaidAmount = input.PaidAmount,
            PaidDate = input.PaidDate,
            Method = input.Method,
            BankName = string.IsNullOrWhiteSpace(input.BankName)
                ? (challan.bankname ?? string.Empty)
                : input.BankName,
            CreatedBy = input.CreatedBy,
            DSNo = challan.depsoiteslipno,
            DSDate = challan.depositdate?.ToString("yyyy-MM-dd"),
            DDNo = input.DDNo,
            DDDate = input.DDDate,
            ChequeNo = input.ChequeNo,
            ChequeDate = input.ChequeDate,
            InstallNo = input.InstallNo,
            PaymentDescription = input.PaymentDescription,
            CreatedOn = DateTime.Now.ToString("yyyy-MM-dd")
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var payments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.customerno == challan.customerno)
            .OrderByDescending(p => p.uId)
            .ToListAsync();

        return PartialView("_ChallanPaymentsTable", payments);
    }
}

