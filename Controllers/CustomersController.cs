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

        var analytics = await _analyticsService.GetAnalyticsAsync();
        var filterOptions = await _analyticsService.GetFilterOptionsAsync();

        ViewBag.FilterOptions = filterOptions;

        return View("Analytics", analytics);
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

    // GET: Customers/Create (also used as edit page when id is provided)
    public async Task<IActionResult> Create(string? id)
    {
        ViewBag.ActiveModule = "Customers";

        // New customer case
        if (string.IsNullOrWhiteSpace(id))
        {
            var newModel = new CustomerCreateViewModel
            {
                Customer = new Customer
                {
                    CreationDate = DateTime.Today
                },
                RequestedProperty = new RequestedProperty()
            };

            return View(newModel);
        }

        // Existing customer (edit) case
        var existingCustomer = await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerNo == id);

        if (existingCustomer == null)
        {
            return NotFound();
        }

        var existingRequested = await _context.RequestedProperties
            .FirstOrDefaultAsync(r => r.CustomerNo == id);

        var model = new CustomerCreateViewModel
        {
            Customer = existingCustomer,
            RequestedProperty = existingRequested ?? new RequestedProperty
            {
                CustomerNo = existingCustomer.CustomerNo
            }
        };

        return View(model);
    }

    // POST: Customers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerCreateViewModel model)
    {
        ViewBag.ActiveModule = "Customers";

        var customer = model.Customer;
        var requestedProperty = model.RequestedProperty;

        // Determine if this is an insert or an update based on CustomerNo
        var isNewCustomer = string.IsNullOrWhiteSpace(customer.CustomerNo);

        // Remove validation for CustomerNo if it's new (auto-generated) or empty
        ModelState.Remove("Customer.CustomerNo");
        ModelState.Remove("model.Customer.CustomerNo");

        if (ModelState.IsValid)
        {
            // Use a transaction so Customer and RequestedProperty are saved together
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (isNewCustomer)
                {
                    // Generate a new CustomerNo following CUST0001, CUST0002... pattern
                    customer.CustomerNo = await GenerateNewCustomerNoAsync();
                    _context.Customers.Add(customer);
                }
                else
                {
                    // Update existing customer record
                    var existingCustomer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.CustomerNo == customer.CustomerNo);

                    if (existingCustomer == null)
                    {
                        ModelState.AddModelError(string.Empty, $"Customer {customer.CustomerNo} not found for updating.");
                        return View(model);
                    }
                    else
                    {
                        // Update existing customer properties
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
                    }
                }

                // Only create/update RequestedProperty if at least one requested field is provided
                var hasRequestedData =
                    !string.IsNullOrWhiteSpace(requestedProperty.ReqProject) ||
                    !string.IsNullOrWhiteSpace(requestedProperty.ReqSize) ||
                    !string.IsNullOrWhiteSpace(requestedProperty.ReqCategory) ||
                    !string.IsNullOrWhiteSpace(requestedProperty.ReqConstruction);

                if (hasRequestedData)
                {
                    // Ensure we always link to the correct CustomerNo
                    requestedProperty.CustomerNo = customer.CustomerNo;

                    var existingRequested = await _context.RequestedProperties
                        .FirstOrDefaultAsync(r => r.CustomerNo == customer.CustomerNo);

                    if (existingRequested == null)
                    {
                        _context.RequestedProperties.Add(requestedProperty);
                    }
                    else
                    {
                        existingRequested.ReqProject = requestedProperty.ReqProject;
                        existingRequested.ReqSize = requestedProperty.ReqSize;
                        existingRequested.ReqCategory = requestedProperty.ReqCategory;
                        existingRequested.ReqConstruction = requestedProperty.ReqConstruction;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Customer information has been saved successfully.";
                return RedirectToAction(nameof(Create), new { id = customer.CustomerNo });
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                var dbMessage = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError(string.Empty, $"Database error while saving customer: {dbMessage}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, $"Unexpected error while saving customer: {ex.Message}");
            }
        }
        else
        {
            // If validation failed, collect all errors to display them more clearly
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            if (errors.Any())
            {
                ModelState.AddModelError(string.Empty, "Please fix the validation errors on all tabs before saving.");
            }
        }

        return View(model);
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

    /// <summary>
    /// Generates a new sequential CustomerNo using the pattern CUST0001, CUST0002, ...
    /// This follows the existing Customers table definition in db.txt (CustomerNo as varchar(50)).
    /// </summary>
    private async Task<string> GenerateNewCustomerNoAsync()
    {
        const string prefix = "CUST";

        // Get all existing numeric parts for codes starting with the prefix
        var existingNumbers = await _context.Customers
            .Where(c => c.CustomerNo.StartsWith(prefix))
            .Select(c => c.CustomerNo.Substring(prefix.Length))
            .ToListAsync();

        var maxNumber = 0;
        foreach (var numString in existingNumbers)
        {
            if (int.TryParse(numString, out var n) && n > maxNumber)
            {
                maxNumber = n;
            }
        }

        var nextNumber = maxNumber + 1;
        return $"{prefix}{nextNumber:D4}";
    }
}
