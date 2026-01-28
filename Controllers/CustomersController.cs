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
        return View(await _context.Customers.OrderByDescending(c => c.CreationDate).ToListAsync());
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

    // GET: Customers/Create (also used as edit page when id is provided, or pre-fill from registration when registrationId is provided)
    public async Task<IActionResult> Create(string? id, int? registrationId)
    {
        ViewBag.ActiveModule = "Customers";

        // Pre-fill from Registration case
        if (registrationId.HasValue)
        {
            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.RegID == registrationId.Value);

            if (registration == null)
            {
                return NotFound();
            }

            // Get project name if ProjectID exists
            string? projectName = null;
            if (registration.ProjectID.HasValue)
            {
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == registration.ProjectID.Value);
                projectName = project?.ProjectName;
            }

            var newModel = new CustomerCreateViewModel
            {
                Customer = new Customer
                {
                    RegistrationID = registration.RegID,
                    FullName = registration.FullName ?? string.Empty,
                    Cnic = registration.CNIC ?? string.Empty,
                    Email = registration.Email,
                    ContactNo = registration.Phone,
                    CreationDate = DateTime.Today
                },
                RequestedProperty = new RequestedProperty
                {
                    ReqProject = projectName,
                    ReqSize = registration.RequestedSize
                }
            };

            // Load dropdown options
            ViewBag.ProjectOptions = await _context.Projects
                .OrderBy(p => p.ProjectName)
                .ToListAsync();

            ViewBag.SizeOptions = await _context.Configurations
                .Where(c => c.ConfigKey == "Size" && c.IsActive)
                .Select(c => c.ConfigValue)
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            ViewBag.CategoryOptions = await _context.Configurations
                .Where(c => c.ConfigKey == "PlotCategory" && c.IsActive)
                .Select(c => c.ConfigValue)
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            ViewBag.FromRegistration = true;
            ViewBag.RegistrationID = registration.RegID;
            return View(newModel);
        }

        // Load dropdown options
        ViewBag.ProjectOptions = await _context.Projects
            .OrderBy(p => p.ProjectName)
            .ToListAsync();

        ViewBag.SizeOptions = await _context.Configurations
            .Where(c => c.ConfigKey == "Size" && c.IsActive)
            .Select(c => c.ConfigValue)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync();

        ViewBag.CategoryOptions = await _context.Configurations
            .Where(c => c.ConfigKey == "PlotCategory" && c.IsActive)
            .Select(c => c.ConfigValue)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync();

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

        // Load dropdown options
        ViewBag.ProjectOptions = await _context.Projects
            .OrderBy(p => p.ProjectName)
            .ToListAsync();

        ViewBag.SizeOptions = await _context.Configurations
            .Where(c => c.ConfigKey == "Size" && c.IsActive)
            .Select(c => c.ConfigValue)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync();

        ViewBag.CategoryOptions = await _context.Configurations
            .Where(c => c.ConfigKey == "PlotCategory" && c.IsActive)
            .Select(c => c.ConfigValue)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync();

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

        // CRITICAL: For new customers, ALWAYS try to generate CustomerNo from the selected project FIRST
        // This ensures CustomerNo is set even if JavaScript didn't run or form field wasn't submitted properly
        if (isNewCustomer && string.IsNullOrWhiteSpace(customer.CustomerNo))
        {
            if (!string.IsNullOrWhiteSpace(requestedProperty?.ReqProject))
            {
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.ProjectName == requestedProperty.ReqProject);

                if (project != null && !string.IsNullOrWhiteSpace(project.Prefix))
                {
                    var prefix = project.Prefix.Trim().ToUpper();
                    
                    // Find all existing customer numbers that start with this prefix
                    var existingNumbers = await _context.Customers
                        .Where(c => c.CustomerNo != null && c.CustomerNo.StartsWith(prefix))
                        .Select(c => c.CustomerNo)
                        .ToListAsync();

                    // Extract numeric parts and find the highest number
                    int nextNumber = 1;
                    foreach (var existingNo in existingNumbers)
                    {
                        if (existingNo != null && existingNo.Length > prefix.Length)
                        {
                            var numericPart = existingNo.Substring(prefix.Length);
                            if (int.TryParse(numericPart, out int num))
                            {
                                if (num >= nextNumber)
                                {
                                    nextNumber = num + 1;
                                }
                            }
                        }
                    }

                    // Format as prefix + 4-digit number (e.g., ABL0001)
                    customer.CustomerNo = $"{prefix}{nextNumber:D4}";
                }
            }
        }

        // CRITICAL FIX: Set RequestedProperty.CustomerNo from Customer.CustomerNo BEFORE removing from ModelState
        // This prevents the [Required] validation error on RequestedProperty.CustomerNo
        if (requestedProperty != null && !string.IsNullOrWhiteSpace(customer.CustomerNo))
        {
            requestedProperty.CustomerNo = customer.CustomerNo;
        }

        // Remove CustomerNo from ModelState validation (we'll validate manually)
        ModelState.Remove("Customer.CustomerNo");
        ModelState.Remove("model.Customer.CustomerNo");
        ModelState.Remove("RequestedProperty.CustomerNo");
        ModelState.Remove("model.RequestedProperty.CustomerNo");
        
        // Manual validation for CustomerNo (required for new customers)
        // Only show error if we still don't have a CustomerNo after trying to generate it
        if (isNewCustomer && string.IsNullOrWhiteSpace(customer.CustomerNo))
        {
            ModelState.AddModelError("Customer.CustomerNo", "Customer No is required. Please select a project in the Requested Property tab to generate it.");
        }

        if (ModelState.IsValid)
        {
            // Use a transaction so Customer and RequestedProperty are saved together
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (isNewCustomer)
                {
                    // Use the CustomerNo from the form (generated from project prefix)
                    // If for some reason it's still empty, fallback to CUST pattern
                    if (string.IsNullOrWhiteSpace(customer.CustomerNo))
                    {
                        customer.CustomerNo = await GenerateNewCustomerNoAsync();
                    }
                    _context.Customers.Add(customer);
                }
                else
                {
                    // Update existing customer record
                    var existingCustomer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.CustomerNo == customer.CustomerNo);

                    if (existingCustomer == null)
                    {
                        // CRITICAL FIX: If customer doesn't exist, treat this as a CREATE, not UPDATE
                        // This happens when CustomerNo is pre-filled but customer doesn't exist yet
                        isNewCustomer = true;
                        _context.Customers.Add(customer);
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
                return RedirectToAction(nameof(CustomersDetail));
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
        // Note: Validation errors are already added above, no need to add duplicate errors here

        // Reload dropdown options for the view
        ViewBag.ProjectOptions = await _context.Projects
            .OrderBy(p => p.ProjectName)
            .ToListAsync();

        ViewBag.SizeOptions = await _context.Configurations
            .Where(c => c.ConfigKey == "Size" && c.IsActive)
            .Select(c => c.ConfigValue)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync();

        ViewBag.CategoryOptions = await _context.Configurations
            .Where(c => c.ConfigKey == "PlotCategory" && c.IsActive)
            .Select(c => c.ConfigValue)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync();

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
    // GET: Customers/GetNextCustomerNo
    [HttpGet]
    public async Task<IActionResult> GetNextCustomerNo([FromQuery] string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            return Json(new { prefix = "", customerNo = "" });
        }

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.ProjectName == projectName);

        if (project == null || string.IsNullOrWhiteSpace(project.Prefix))
        {
            return Json(new { prefix = "", customerNo = "" });
        }

        var prefix = project.Prefix.Trim().ToUpper();
        
        // Find all existing customer numbers that start with this prefix
        var existingNumbers = await _context.Customers
            .Where(c => c.CustomerNo != null && c.CustomerNo.StartsWith(prefix))
            .Select(c => c.CustomerNo)
            .ToListAsync();

        // Extract numeric parts and find the highest number
        int nextNumber = 1;
        foreach (var existingNo in existingNumbers)
        {
            if (existingNo != null && existingNo.Length > prefix.Length)
            {
                var numericPart = existingNo.Substring(prefix.Length);
                if (int.TryParse(numericPart, out int num))
                {
                    if (num >= nextNumber)
                    {
                        nextNumber = num + 1;
                    }
                }
            }
        }

        // Format as prefix + 4-digit number (e.g., ABL0001)
        var customerNo = $"{prefix}{nextNumber:D4}";

        return Json(new { prefix = prefix, customerNo = customerNo });
    }

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
