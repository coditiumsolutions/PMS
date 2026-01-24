using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.ViewModels;

namespace PMS.Web.Services;

/// <summary>
/// Service for customer analytics and data aggregation
/// </summary>
public class CustomerAnalyticsService
{
    private readonly PMSDbContext _context;

    public CustomerAnalyticsService(PMSDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get comprehensive analytics data with optional filters
    /// </summary>
    public async Task<CustomerAnalyticsViewModel> GetAnalyticsAsync(AnalyticsFilterModel? filter = null)
    {
        var query = _context.Customers.AsQueryable();

        // Apply filters
        if (filter != null)
        {
            if (filter.StartDate.HasValue)
                query = query.Where(c => c.CreationDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(c => c.CreationDate <= filter.EndDate.Value);

            if (!string.IsNullOrWhiteSpace(filter.City))
                query = query.Where(c => c.PresCity == filter.City || c.PremCity == filter.City);

            if (!string.IsNullOrWhiteSpace(filter.Country))
                query = query.Where(c => c.PresCountry == filter.Country || c.PremCountry == filter.Country);

            if (!string.IsNullOrWhiteSpace(filter.Gender))
                query = query.Where(c => c.Gender == filter.Gender);

            if (!string.IsNullOrWhiteSpace(filter.CreatedBy))
                query = query.Where(c => c.CreatedBy == filter.CreatedBy);
        }

        var customers = await query.ToListAsync();
        var totalCustomers = customers.Count;

        return new CustomerAnalyticsViewModel
        {
            KPIs = GetKPIs(customers, totalCustomers),
            GrowthAnalysis = GetGrowthAnalysis(customers, totalCustomers),
            DemographicAnalysis = GetDemographicAnalysis(customers, totalCustomers),
            LocationAnalysis = GetLocationAnalysis(customers, totalCustomers),
            RegistrationAnalysis = GetRegistrationAnalysis(customers, totalCustomers),
            DataQuality = GetDataQualityInsights(customers, totalCustomers)
        };
    }

    private CustomerKPIs GetKPIs(List<Models.Customer> customers, int totalCustomers)
    {
        var today = DateTime.Today;
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);
        var thisYearStart = new DateTime(today.Year, 1, 1);

        var newThisMonth = customers.Count(c => c.CreationDate >= thisMonthStart);
        var newThisYear = customers.Count(c => c.CreationDate >= thisYearStart);

        var activeCities = customers
            .Where(c => !string.IsNullOrWhiteSpace(c.PresCity) || !string.IsNullOrWhiteSpace(c.PremCity))
            .SelectMany(c => new[] { c.PresCity, c.PremCity })
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .Count();

        var activeCountries = customers
            .Where(c => !string.IsNullOrWhiteSpace(c.PresCountry) || !string.IsNullOrWhiteSpace(c.PremCountry))
            .SelectMany(c => new[] { c.PresCountry, c.PremCountry })
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .Count();

        var customersWithEmail = customers.Count(c => !string.IsNullOrWhiteSpace(c.Email));
        var customersWithContact = customers.Count(c => !string.IsNullOrWhiteSpace(c.ContactNo));

        return new CustomerKPIs
        {
            TotalCustomers = totalCustomers,
            NewThisMonth = newThisMonth,
            NewThisYear = newThisYear,
            ActiveCities = activeCities,
            ActiveCountries = activeCountries,
            CustomersWithEmail = customersWithEmail,
            CustomersWithContact = customersWithContact
        };
    }

    private GrowthAnalysisData GetGrowthAnalysis(List<Models.Customer> customers, int totalCustomers)
    {
        var monthlyGrowth = new List<MonthlyGrowthData>();
        var yearlyGrowth = new List<YearlyGrowthData>();

        // Get monthly growth for last 12 months
        var today = DateTime.Today;
        var cumulativeCount = 0;

        for (int i = 11; i >= 0; i--)
        {
            var monthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            var monthName = monthStart.ToString("MMM yyyy");
            var monthYear = monthStart.ToString("yyyy-MM");

            var monthCount = customers.Count(c => c.CreationDate >= monthStart && c.CreationDate < monthEnd);
            cumulativeCount += monthCount;

            monthlyGrowth.Add(new MonthlyGrowthData
            {
                Month = monthName,
                MonthYear = monthYear,
                Count = monthCount,
                CumulativeCount = cumulativeCount
            });
        }

        // Get yearly growth
        var years = customers
            .Select(c => c.CreationDate.Year)
            .Distinct()
            .OrderBy(y => y)
            .ToList();

        int? previousYearCount = null;
        foreach (var year in years)
        {
            var yearCount = customers.Count(c => c.CreationDate.Year == year);
            var growthPercentage = previousYearCount.HasValue && previousYearCount.Value > 0
                ? ((yearCount - previousYearCount.Value) / (decimal)previousYearCount.Value) * 100
                : 0;

            yearlyGrowth.Add(new YearlyGrowthData
            {
                Year = year,
                Count = yearCount,
                GrowthPercentage = growthPercentage
            });

            previousYearCount = yearCount;
        }

        // Calculate growth rates
        var lastMonthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
        var lastMonthEnd = new DateTime(today.Year, today.Month, 1);
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);
        var thisMonthEnd = thisMonthStart.AddMonths(1);

        var lastMonthCount = customers.Count(c => c.CreationDate >= lastMonthStart && c.CreationDate < lastMonthEnd);
        var thisMonthCount = customers.Count(c => c.CreationDate >= thisMonthStart && c.CreationDate < thisMonthEnd);

        var growthRateThisMonth = lastMonthCount > 0
            ? ((thisMonthCount - lastMonthCount) / (decimal)lastMonthCount) * 100
            : thisMonthCount > 0 ? 100 : 0;

        var lastYearStart = new DateTime(today.Year - 1, 1, 1);
        var lastYearEnd = new DateTime(today.Year, 1, 1);
        var thisYearStart = new DateTime(today.Year, 1, 1);
        var thisYearEnd = new DateTime(today.Year + 1, 1, 1);

        var lastYearCount = customers.Count(c => c.CreationDate >= lastYearStart && c.CreationDate < lastYearEnd);
        var thisYearCount = customers.Count(c => c.CreationDate >= thisYearStart && c.CreationDate < thisYearEnd);

        var growthRateThisYear = lastYearCount > 0
            ? ((thisYearCount - lastYearCount) / (decimal)lastYearCount) * 100
            : thisYearCount > 0 ? 100 : 0;

        return new GrowthAnalysisData
        {
            MonthlyGrowth = monthlyGrowth,
            YearlyGrowth = yearlyGrowth,
            GrowthRateThisMonth = growthRateThisMonth,
            GrowthRateThisYear = growthRateThisYear
        };
    }

    private DemographicAnalysisData GetDemographicAnalysis(List<Models.Customer> customers, int totalCustomers)
    {
        // Gender distribution
        var genderGroups = customers
            .GroupBy(c => string.IsNullOrWhiteSpace(c.Gender) ? "Not Specified" : c.Gender)
            .Select(g => new GenderDistribution
            {
                Gender = g.Key,
                Count = g.Count(),
                Percentage = totalCustomers > 0 ? (g.Count() / (decimal)totalCustomers) * 100 : 0
            })
            .OrderByDescending(g => g.Count)
            .ToList();

        // City distribution (using Present City)
        var cityGroups = customers
            .Where(c => !string.IsNullOrWhiteSpace(c.PresCity))
            .GroupBy(c => c.PresCity!)
            .Select(g => new CityDistribution
            {
                City = g.Key,
                Count = g.Count(),
                Percentage = totalCustomers > 0 ? (g.Count() / (decimal)totalCustomers) * 100 : 0
            })
            .OrderByDescending(c => c.Count)
            .Take(20) // Top 20 cities
            .ToList();

        // Country distribution (using Present Country)
        var countryGroups = customers
            .Where(c => !string.IsNullOrWhiteSpace(c.PresCountry))
            .GroupBy(c => c.PresCountry!)
            .Select(g => new CountryDistribution
            {
                Country = g.Key,
                Count = g.Count(),
                Percentage = totalCustomers > 0 ? (g.Count() / (decimal)totalCustomers) * 100 : 0
            })
            .OrderByDescending(c => c.Count)
            .ToList();

        return new DemographicAnalysisData
        {
            GenderDistribution = genderGroups,
            CityDistribution = cityGroups,
            CountryDistribution = countryGroups
        };
    }

    private LocationAnalysisData GetLocationAnalysis(List<Models.Customer> customers, int totalCustomers)
    {
        // City comparison (Present vs Permanent)
        var allCities = customers
            .Where(c => !string.IsNullOrWhiteSpace(c.PresCity) || !string.IsNullOrWhiteSpace(c.PremCity))
            .SelectMany(c => new[] { c.PresCity, c.PremCity })
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToList();

        var cityComparison = allCities
            .Select(city => new CityComparison
            {
                City = city!,
                PresentAddressCount = customers.Count(c => c.PresCity == city),
                PermanentAddressCount = customers.Count(c => c.PremCity == city)
            })
            .OrderByDescending(c => c.PresentAddressCount + c.PermanentAddressCount)
            .Take(15)
            .ToList();

        // Top cities
        var topCities = customers
            .Where(c => !string.IsNullOrWhiteSpace(c.PresCity))
            .GroupBy(c => c.PresCity!)
            .Select(g => new TopCityData
            {
                City = g.Key,
                CustomerCount = g.Count(),
                Percentage = totalCustomers > 0 ? (g.Count() / (decimal)totalCustomers) * 100 : 0
            })
            .OrderByDescending(c => c.CustomerCount)
            .Take(10)
            .ToList();

        // Same vs different city
        var sameCityCount = customers.Count(c =>
            !string.IsNullOrWhiteSpace(c.PresCity) &&
            !string.IsNullOrWhiteSpace(c.PremCity) &&
            c.PresCity == c.PremCity);

        var differentCityCount = customers.Count(c =>
            !string.IsNullOrWhiteSpace(c.PresCity) &&
            !string.IsNullOrWhiteSpace(c.PremCity) &&
            c.PresCity != c.PremCity);

        return new LocationAnalysisData
        {
            CityComparison = cityComparison,
            TopCities = topCities,
            SameCityCount = sameCityCount,
            DifferentCityCount = differentCityCount
        };
    }

    private RegistrationAnalysisData GetRegistrationAnalysis(List<Models.Customer> customers, int totalCustomers)
    {
        // Date range registrations (Last 7 days, 30 days, 90 days, 1 year)
        var today = DateTime.Today;
        var dateRanges = new List<DateRangeRegistration>
        {
            new()
            {
                StartDate = today.AddDays(-7),
                EndDate = today,
                RangeLabel = "Last 7 Days",
                Count = customers.Count(c => c.CreationDate >= today.AddDays(-7))
            },
            new()
            {
                StartDate = today.AddDays(-30),
                EndDate = today,
                RangeLabel = "Last 30 Days",
                Count = customers.Count(c => c.CreationDate >= today.AddDays(-30))
            },
            new()
            {
                StartDate = today.AddDays(-90),
                EndDate = today,
                RangeLabel = "Last 90 Days",
                Count = customers.Count(c => c.CreationDate >= today.AddDays(-90))
            },
            new()
            {
                StartDate = today.AddYears(-1),
                EndDate = today,
                RangeLabel = "Last Year",
                Count = customers.Count(c => c.CreationDate >= today.AddYears(-1))
            }
        };

        // Created by analysis
        var createdByGroups = customers
            .GroupBy(c => string.IsNullOrWhiteSpace(c.CreatedBy) ? "System/Unknown" : c.CreatedBy)
            .Select(g => new CreatedByAnalysis
            {
                CreatedBy = g.Key,
                Count = g.Count(),
                Percentage = totalCustomers > 0 ? (g.Count() / (decimal)totalCustomers) * 100 : 0
            })
            .OrderByDescending(c => c.Count)
            .ToList();

        // Daily registrations (last 30 days)
        var dailyRegistrations = new List<DailyRegistrationData>();
        for (int i = 29; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var count = customers.Count(c => c.CreationDate.Date == date.Date);

            dailyRegistrations.Add(new DailyRegistrationData
            {
                Date = date,
                DateLabel = date.ToString("MMM dd"),
                Count = count
            });
        }

        return new RegistrationAnalysisData
        {
            DateRangeRegistrations = dateRanges,
            CreatedByAnalysis = createdByGroups,
            DailyRegistrations = dailyRegistrations
        };
    }

    private DataQualityInsights GetDataQualityInsights(List<Models.Customer> customers, int totalCustomers)
    {
        var missingEmails = customers.Count(c => string.IsNullOrWhiteSpace(c.Email));
        var missingContactNumbers = customers.Count(c => string.IsNullOrWhiteSpace(c.ContactNo));
        var missingBoth = customers.Count(c =>
            string.IsNullOrWhiteSpace(c.Email) && string.IsNullOrWhiteSpace(c.ContactNo));

        // Find duplicate CNICs
        var duplicateCNICs = customers
            .Where(c => !string.IsNullOrWhiteSpace(c.Cnic))
            .GroupBy(c => c.Cnic!)
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicateCNICData
            {
                CNIC = g.Key,
                Count = g.Count(),
                CustomerNos = g.Select(c => c.CustomerNo).ToList()
            })
            .ToList();

        var duplicateCNICCount = duplicateCNICs.Sum(d => d.Count - 1); // Count duplicates, not originals

        // Calculate data completeness
        var requiredFields = new[]
        {
            nameof(Models.Customer.FullName),
            nameof(Models.Customer.FatherName),
            nameof(Models.Customer.Cnic),
            nameof(Models.Customer.ContactNo),
            nameof(Models.Customer.Email)
        };

        var totalFields = totalCustomers * requiredFields.Length;
        var completedFields = 0;

        foreach (var customer in customers)
        {
            if (!string.IsNullOrWhiteSpace(customer.FullName)) completedFields++;
            if (!string.IsNullOrWhiteSpace(customer.FatherName)) completedFields++;
            if (!string.IsNullOrWhiteSpace(customer.Cnic)) completedFields++;
            if (!string.IsNullOrWhiteSpace(customer.ContactNo)) completedFields++;
            if (!string.IsNullOrWhiteSpace(customer.Email)) completedFields++;
        }

        var dataCompletenessPercentage = totalFields > 0
            ? (completedFields / (decimal)totalFields) * 100
            : 0;

        return new DataQualityInsights
        {
            MissingEmails = missingEmails,
            MissingContactNumbers = missingContactNumbers,
            MissingBoth = missingBoth,
            DuplicateCNICs = duplicateCNICCount,
            DuplicateCNICDetails = duplicateCNICs,
            DataCompletenessPercentage = dataCompletenessPercentage
        };
    }

    /// <summary>
    /// Get available filter options (cities, countries, genders, created by)
    /// </summary>
    public async Task<Dictionary<string, List<string>>> GetFilterOptionsAsync()
    {
        var customers = await _context.Customers.ToListAsync();

        return new Dictionary<string, List<string>>
        {
            ["Cities"] = customers
                .Where(c => !string.IsNullOrWhiteSpace(c.PresCity))
                .Select(c => c.PresCity!)
                .Distinct()
                .OrderBy(c => c)
                .ToList(),
            ["Countries"] = customers
                .Where(c => !string.IsNullOrWhiteSpace(c.PresCountry))
                .Select(c => c.PresCountry!)
                .Distinct()
                .OrderBy(c => c)
                .ToList(),
            ["Genders"] = customers
                .Where(c => !string.IsNullOrWhiteSpace(c.Gender))
                .Select(c => c.Gender!)
                .Distinct()
                .OrderBy(c => c)
                .ToList(),
            ["CreatedBy"] = customers
                .Where(c => !string.IsNullOrWhiteSpace(c.CreatedBy))
                .Select(c => c.CreatedBy!)
                .Distinct()
                .OrderBy(c => c)
                .ToList()
        };
    }
}
