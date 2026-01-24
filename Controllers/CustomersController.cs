using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Models;
using PMS.Web.Services;
using PMS.Web.ViewModels;

namespace PMS.Web.Controllers;

public class CustomersController : Controller
{
    private readonly PMSDbContext _context;
    private readonly AuditLogService _auditLogService;
    private readonly CustomerAnalyticsService _analyticsService;

    public CustomersController(PMSDbContext context, AuditLogService auditLogService, CustomerAnalyticsService analyticsService)
    {
        _context = context;
        _auditLogService = auditLogService;
        _analyticsService = analyticsService;
    }

    // GET: Customers
    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveModule = "Customers";
        
        var analytics = await _analyticsService.GetAnalyticsAsync();
        var filterOptions = await _analyticsService.GetFilterOptionsAsync();
        
        ViewBag.FilterOptions = filterOptions;
        
        return View("Analytics", analytics);
    }

    // GET: Customers/CustomersDetail
    public async Task<IActionResult> CustomersDetail()
    {
        ViewBag.ActiveModule = "Customers";
        return View(await _context.Customers.OrderBy(c => c.CustomerNo).ToListAsync());
    }

    // GET: Customers/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        ViewBag.ActiveModule = "Customers";
        
        var customers = await _context.Customers.ToListAsync();
        
        // Total customers
        ViewBag.TotalCustomers = customers.Count;
        
        // Gender distribution
        ViewBag.MaleCustomers = customers.Count(c => c.Gender?.ToLower() == "male");
        ViewBag.FemaleCustomers = customers.Count(c => c.Gender?.ToLower() == "female");
        ViewBag.OtherGenderCustomers = customers.Count(c => string.IsNullOrEmpty(c.Gender) || 
            (c.Gender?.ToLower() != "male" && c.Gender?.ToLower() != "female"));
        
        // Project-wise customers (grouping by CreatedBy or a project identifier)
        // For now, we'll group by CreatedBy as a proxy for project assignment
        // This can be enhanced later when a proper Project relationship is added
        var customersByProject = customers
            .Where(c => !string.IsNullOrEmpty(c.CreatedBy))
            .GroupBy(c => c.CreatedBy ?? "Unassigned")
            .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();
        
        ViewBag.CustomersByProject = customersByProject;
        
        // Customers by city
        var customersByCity = customers
            .Where(c => !string.IsNullOrEmpty(c.PresCity))
            .GroupBy(c => c.PresCity ?? "Unknown")
            .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .Take(10)
            .ToList();
        
        ViewBag.CustomersByCity = customersByCity;
        
        // Recent customers (last 30 days)
        var thirtyDaysAgo = DateTime.Today.AddDays(-30);
        ViewBag.RecentCustomers = customers.Count(c => c.CreationDate >= thirtyDaysAgo);
        
        // Customers created this month
        var thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        ViewBag.ThisMonthCustomers = customers.Count(c => c.CreationDate >= thisMonth);
        
        // Monthly registration trend (last 6 months)
        var monthlyData = new List<KeyValuePair<string, int>>();
        for (int i = 5; i >= 0; i--)
        {
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            var monthName = monthStart.ToString("MMM yyyy");
            var count = customers.Count(c => c.CreationDate >= monthStart && c.CreationDate < monthEnd);
            monthlyData.Add(new KeyValuePair<string, int>(monthName, count));
        }
        ViewBag.MonthlyTrend = monthlyData;
        
        return View();
    }

    // GET: Customers/Details/CUST001
    public async Task<IActionResult> Details(string? id)
    {
        ViewBag.ActiveModule = "Customers";
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(m => m.CustomerNo == id);
        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    // GET: Customers/CustomersDetails/CUST001
    public async Task<IActionResult> CustomersDetails(string? id, string? returnUrl = null)
    {
        ViewBag.ActiveModule = "Customers";
        ViewBag.UseCustomersDetails = true;
        ViewBag.ReturnUrl = returnUrl; // Store return URL for navigation
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(m => m.CustomerNo == id);
        if (customer == null)
        {
            return NotFound();
        }

        return View("Details", customer);
    }

    // GET: Customers/Create
    public IActionResult Create()
    {
        ViewBag.ActiveModule = "Customers";
        return View();
    }

    // POST: Customers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CustomerNo,FullName,FatherName,Cnic,ContactNo,Email,Gender,PresAddress,PremAddress,PresCity,PremCity,PresCountry,PremCountry,CreationDate,CreatedBy")] Customer customer)
    {
        ViewBag.ActiveModule = "Customers";
        
        // Check if CustomerNo already exists
        if (await _context.Customers.AnyAsync(c => c.CustomerNo == customer.CustomerNo))
        {
            ModelState.AddModelError("CustomerNo", "Customer No already exists. Please use a different Customer No.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Add(customer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException?.Message.Contains("PRIMARY KEY") == true || 
                    ex.InnerException?.Message.Contains("duplicate key") == true)
                {
                    ModelState.AddModelError("CustomerNo", "Customer No already exists. Please use a different Customer No.");
                }
                else
                {
                    ModelState.AddModelError("", "An error occurred while saving. Please try again.");
                }
            }
        }
        return View(customer);
    }

    // GET: Customers/Edit/CUST001
    public async Task<IActionResult> Edit(string? id)
    {
        ViewBag.ActiveModule = "Customers";
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }
        return View(customer);
    }

    // POST: Customers/Edit/CUST001
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("CustomerNo,FullName,FatherName,Cnic,ContactNo,Email,Gender,PresAddress,PremAddress,PresCity,PremCity,PresCountry,PremCountry,CreationDate,CreatedBy")] Customer customer)
    {
        ViewBag.ActiveModule = "Customers";
        if (id != customer.CustomerNo)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            // Use a transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get the original customer record before update
                var oldCustomer = await _context.Customers.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CustomerNo == id);
                
                if (oldCustomer == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                // Get the existing customer from database
                var existingCustomer = await _context.Customers.FindAsync(id);
                if (existingCustomer == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                // Update only the allowed properties (exclude Uid which is identity)
                existingCustomer.FullName = customer.FullName;
                existingCustomer.FatherName = customer.FatherName;
                existingCustomer.Cnic = customer.Cnic;
                existingCustomer.ContactNo = customer.ContactNo;
                existingCustomer.Email = customer.Email;
                existingCustomer.Gender = customer.Gender;
                existingCustomer.PresAddress = customer.PresAddress;
                existingCustomer.PremAddress = customer.PremAddress;
                existingCustomer.PresCity = customer.PresCity;
                existingCustomer.PremCity = customer.PremCity;
                existingCustomer.PresCountry = customer.PresCountry;
                existingCustomer.PremCountry = customer.PremCountry;
                existingCustomer.CreationDate = customer.CreationDate;
                existingCustomer.CreatedBy = customer.CreatedBy;
                
                // Log the update action (within same transaction)
                _auditLogService.LogCustomerUpdate(oldCustomer, existingCustomer, User?.Identity?.Name);
                
                // Save both changes in the same transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                if (!CustomerExists(customer.CustomerNo))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(customer);
    }

    // GET: Customers/CustomersEdit/CUST001
    public async Task<IActionResult> CustomersEdit(string? id, string? returnUrl = null)
    {
        ViewBag.ActiveModule = "Customers";
        ViewBag.UseCustomersEdit = true;
        ViewBag.ReturnUrl = returnUrl; // Store return URL for navigation
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }
        return View("Edit", customer);
    }

    // POST: Customers/CustomersEdit/CUST001
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CustomersEdit(string id, [Bind("CustomerNo,FullName,FatherName,Cnic,ContactNo,Email,Gender,PresAddress,PremAddress,PresCity,PremCity,PresCountry,PremCountry,CreationDate,CreatedBy")] Customer customer, string? returnUrl = null)
    {
        ViewBag.ActiveModule = "Customers";
        if (id != customer.CustomerNo)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            // Use a transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get the original customer record before update
                var oldCustomer = await _context.Customers.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CustomerNo == id);
                
                if (oldCustomer == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                // Get the existing customer from database
                var existingCustomer = await _context.Customers.FindAsync(id);
                if (existingCustomer == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                // Update only the allowed properties (exclude Uid which is identity)
                existingCustomer.FullName = customer.FullName;
                existingCustomer.FatherName = customer.FatherName;
                existingCustomer.Cnic = customer.Cnic;
                existingCustomer.ContactNo = customer.ContactNo;
                existingCustomer.Email = customer.Email;
                existingCustomer.Gender = customer.Gender;
                existingCustomer.PresAddress = customer.PresAddress;
                existingCustomer.PremAddress = customer.PremAddress;
                existingCustomer.PresCity = customer.PresCity;
                existingCustomer.PremCity = customer.PremCity;
                existingCustomer.PresCountry = customer.PresCountry;
                existingCustomer.PremCountry = customer.PremCountry;
                existingCustomer.CreationDate = customer.CreationDate;
                existingCustomer.CreatedBy = customer.CreatedBy;
                
                // Log the update action (within same transaction)
                _auditLogService.LogCustomerUpdate(oldCustomer, existingCustomer, User?.Identity?.Name);
                
                // Save both changes in the same transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                if (!CustomerExists(customer.CustomerNo))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            // Redirect based on returnUrl
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl == "CustomersOperation")
            {
                return RedirectToAction(nameof(CustomersOperation));
            }
            return RedirectToAction(nameof(CustomersDetail));
        }
        ViewBag.ReturnUrl = returnUrl; // Preserve returnUrl on validation error
        return View("Edit", customer);
    }

    // GET: Customers/Delete/CUST001
    public async Task<IActionResult> Delete(string? id)
    {
        ViewBag.ActiveModule = "Customers";
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(m => m.CustomerNo == id);
        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    // POST: Customers/Delete/CUST001
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        ViewBag.ActiveModule = "Customers";
        
        // Use a transaction to ensure atomicity
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                // Log the delete action (within same transaction)
                _auditLogService.LogCustomerDelete(customer, User?.Identity?.Name);
                
                // Remove the customer
                _context.Customers.Remove(customer);
                
                // Save both changes in the same transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Customers/Summary
    public async Task<IActionResult> Summary()
    {
        ViewBag.ActiveModule = "Customers";
        
        var customers = await _context.Customers.OrderBy(c => c.CustomerNo).ToListAsync();
        
        // Total customers
        ViewBag.TotalCustomers = customers.Count;
        
        // Project-wise customers (grouping by CreatedBy as a proxy for project assignment)
        var customersByProject = customers
            .GroupBy(c => !string.IsNullOrEmpty(c.CreatedBy) ? c.CreatedBy : "Unassigned")
            .Select(g => new KeyValuePair<string, List<Customer>>(
                g.Key, 
                g.OrderBy(c => c.CustomerNo).ToList()
            ))
            .OrderByDescending(x => x.Value.Count)
            .ThenBy(x => x.Key)
            .ToList();
        
        ViewBag.CustomersByProject = customersByProject;
        
        // Gender distribution
        ViewBag.MaleCustomers = customers.Count(c => c.Gender?.ToLower() == "male");
        ViewBag.FemaleCustomers = customers.Count(c => c.Gender?.ToLower() == "female");
        ViewBag.OtherGenderCustomers = customers.Count(c => string.IsNullOrEmpty(c.Gender) || 
            (c.Gender?.ToLower() != "male" && c.Gender?.ToLower() != "female"));
        
        // City-wise distribution
        var customersByCity = customers
            .Where(c => !string.IsNullOrEmpty(c.PresCity))
            .GroupBy(c => c.PresCity ?? "Unknown")
            .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .ToList();
        
        ViewBag.CustomersByCity = customersByCity;
        
        return View();
    }

    // GET: Customers/CustomersOperation
    public IActionResult CustomersOperation()
    {
        ViewBag.ActiveModule = "Customers";
        return View();
    }

    // POST: Customers/CustomersOperation/Search
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SearchCustomers([FromBody] string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Json(new { success = false, message = "Please enter a search term" });
        }

        var trimmedSearch = searchTerm.Trim().ToLower();
        
        var customers = await _context.Customers
            .Where(c => 
                (!string.IsNullOrEmpty(c.CustomerNo) && c.CustomerNo.ToLower().Contains(trimmedSearch)) ||
                (!string.IsNullOrEmpty(c.FullName) && c.FullName.ToLower().Contains(trimmedSearch)) ||
                (!string.IsNullOrEmpty(c.Cnic) && c.Cnic.ToLower().Contains(trimmedSearch)))
            .OrderBy(c => c.CustomerNo)
            .ToListAsync();

        return Json(new { success = true, customers = customers });
    }

    // GET: Customers/History
    public async Task<IActionResult> History(string? customerId = null)
    {
        ViewBag.ActiveModule = "Customers";

        // When no customerId is provided, keep the grid empty
        if (string.IsNullOrWhiteSpace(customerId))
        {
            ViewBag.CustomerId = null;
            return View(System.Linq.Enumerable.Empty<CustomerAuditLog>());
        }

        // Filter by customer ID and show only relevant records
        ViewBag.CustomerId = customerId;
        var auditLogs = await _context.CustomerAuditLogs
            .Where(a => a.CustomerID == customerId)
            .OrderByDescending(a => a.ActionDate)
            .ToListAsync();

        return View(auditLogs);
    }

    // GET: Customers/Analytics
    public async Task<IActionResult> Analytics()
    {
        ViewBag.ActiveModule = "Customers";
        
        var analytics = await _analyticsService.GetAnalyticsAsync();
        var filterOptions = await _analyticsService.GetFilterOptionsAsync();
        
        ViewBag.FilterOptions = filterOptions;
        
        return View(analytics);
    }

    // POST: Customers/Analytics/Filter
    [HttpPost]
    public async Task<IActionResult> GetFilteredAnalytics([FromBody] AnalyticsFilterModel filter)
    {
        var analytics = await _analyticsService.GetAnalyticsAsync(filter);
        return Json(analytics);
    }

    // GET: Customers/Analytics/ChartData/Growth
    [HttpGet]
    public async Task<IActionResult> GetGrowthChartData([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var filter = new AnalyticsFilterModel
        {
            StartDate = startDate,
            EndDate = endDate
        };
        
        var analytics = await _analyticsService.GetAnalyticsAsync(filter);
        
        return Json(new
        {
            labels = analytics.GrowthAnalysis.MonthlyGrowth.Select(m => m.Month).ToArray(),
            data = analytics.GrowthAnalysis.MonthlyGrowth.Select(m => m.Count).ToArray(),
            cumulativeData = analytics.GrowthAnalysis.MonthlyGrowth.Select(m => m.CumulativeCount).ToArray()
        });
    }

    // GET: Customers/Analytics/ChartData/Gender
    [HttpGet]
    public async Task<IActionResult> GetGenderChartData([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var filter = new AnalyticsFilterModel
        {
            StartDate = startDate,
            EndDate = endDate
        };
        
        var analytics = await _analyticsService.GetAnalyticsAsync(filter);
        
        return Json(new
        {
            labels = analytics.DemographicAnalysis.GenderDistribution.Select(g => g.Gender).ToArray(),
            data = analytics.DemographicAnalysis.GenderDistribution.Select(g => g.Count).ToArray()
        });
    }

    // GET: Customers/Analytics/ChartData/City
    [HttpGet]
    public async Task<IActionResult> GetCityChartData([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int top = 10)
    {
        var filter = new AnalyticsFilterModel
        {
            StartDate = startDate,
            EndDate = endDate
        };
        
        var analytics = await _analyticsService.GetAnalyticsAsync(filter);
        
        var topCities = analytics.DemographicAnalysis.CityDistribution.Take(top).ToList();
        
        return Json(new
        {
            labels = topCities.Select(c => c.City).ToArray(),
            data = topCities.Select(c => c.Count).ToArray()
        });
    }

    // GET: Customers/Analytics/ChartData/LocationComparison
    [HttpGet]
    public async Task<IActionResult> GetLocationComparisonData([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var filter = new AnalyticsFilterModel
        {
            StartDate = startDate,
            EndDate = endDate
        };
        
        var analytics = await _analyticsService.GetAnalyticsAsync(filter);
        
        var topCities = analytics.LocationAnalysis.CityComparison.Take(10).ToList();
        
        return Json(new
        {
            labels = topCities.Select(c => c.City).ToArray(),
            presentData = topCities.Select(c => c.PresentAddressCount).ToArray(),
            permanentData = topCities.Select(c => c.PermanentAddressCount).ToArray()
        });
    }

    // GET: Customers/Analytics/ChartData/DailyRegistrations
    [HttpGet]
    public async Task<IActionResult> GetDailyRegistrationsData([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var filter = new AnalyticsFilterModel
        {
            StartDate = startDate,
            EndDate = endDate
        };
        
        var analytics = await _analyticsService.GetAnalyticsAsync(filter);
        
        return Json(new
        {
            labels = analytics.RegistrationAnalysis.DailyRegistrations.Select(d => d.DateLabel).ToArray(),
            data = analytics.RegistrationAnalysis.DailyRegistrations.Select(d => d.Count).ToArray()
        });
    }

    // GET: Customers/Analytics/FilterOptions
    [HttpGet]
    public async Task<IActionResult> GetFilterOptions()
    {
        var options = await _analyticsService.GetFilterOptionsAsync();
        return Json(options);
    }

    private bool CustomerExists(string id)
    {
        return _context.Customers.Any(e => e.CustomerNo == id);
    }
}
