namespace PMS.Web.ViewModels;

/// <summary>
/// Main view model for Customer Analytics page
/// </summary>
public class CustomerAnalyticsViewModel
{
    public CustomerKPIs KPIs { get; set; } = new();
    public GrowthAnalysisData GrowthAnalysis { get; set; } = new();
    public DemographicAnalysisData DemographicAnalysis { get; set; } = new();
    public LocationAnalysisData LocationAnalysis { get; set; } = new();
    public RegistrationAnalysisData RegistrationAnalysis { get; set; } = new();
    public DataQualityInsights DataQuality { get; set; } = new();
}

/// <summary>
/// Key Performance Indicators for customers
/// </summary>
public class CustomerKPIs
{
    public int TotalCustomers { get; set; }
    public int NewThisMonth { get; set; }
    public int NewThisYear { get; set; }
    public int ActiveCities { get; set; }
    public int ActiveCountries { get; set; }
    public int CustomersWithEmail { get; set; }
    public int CustomersWithContact { get; set; }
}

/// <summary>
/// Customer growth trend data
/// </summary>
public class GrowthAnalysisData
{
    public List<MonthlyGrowthData> MonthlyGrowth { get; set; } = new();
    public List<YearlyGrowthData> YearlyGrowth { get; set; } = new();
    public decimal GrowthRateThisMonth { get; set; }
    public decimal GrowthRateThisYear { get; set; }
}

public class MonthlyGrowthData
{
    public string Month { get; set; } = string.Empty;
    public string MonthYear { get; set; } = string.Empty;
    public int Count { get; set; }
    public int CumulativeCount { get; set; }
}

public class YearlyGrowthData
{
    public int Year { get; set; }
    public int Count { get; set; }
    public decimal GrowthPercentage { get; set; }
}

/// <summary>
/// Demographic analysis data
/// </summary>
public class DemographicAnalysisData
{
    public List<GenderDistribution> GenderDistribution { get; set; } = new();
    public List<CityDistribution> CityDistribution { get; set; } = new();
    public List<CountryDistribution> CountryDistribution { get; set; } = new();
}

public class GenderDistribution
{
    public string Gender { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class CityDistribution
{
    public string City { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class CountryDistribution
{
    public string Country { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Location analysis data
/// </summary>
public class LocationAnalysisData
{
    public List<CityComparison> CityComparison { get; set; } = new();
    public List<TopCityData> TopCities { get; set; } = new();
    public int SameCityCount { get; set; }
    public int DifferentCityCount { get; set; }
}

public class CityComparison
{
    public string City { get; set; } = string.Empty;
    public int PresentAddressCount { get; set; }
    public int PermanentAddressCount { get; set; }
}

public class TopCityData
{
    public string City { get; set; } = string.Empty;
    public int CustomerCount { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Registration analysis data
/// </summary>
public class RegistrationAnalysisData
{
    public List<DateRangeRegistration> DateRangeRegistrations { get; set; } = new();
    public List<CreatedByAnalysis> CreatedByAnalysis { get; set; } = new();
    public List<DailyRegistrationData> DailyRegistrations { get; set; } = new();
}

public class DateRangeRegistration
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string RangeLabel { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class CreatedByAnalysis
{
    public string CreatedBy { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class DailyRegistrationData
{
    public DateTime Date { get; set; }
    public string DateLabel { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Data quality insights
/// </summary>
public class DataQualityInsights
{
    public int MissingEmails { get; set; }
    public int MissingContactNumbers { get; set; }
    public int MissingBoth { get; set; }
    public int DuplicateCNICs { get; set; }
    public List<DuplicateCNICData> DuplicateCNICDetails { get; set; } = new();
    public decimal DataCompletenessPercentage { get; set; }
}

public class DuplicateCNICData
{
    public string CNIC { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<string> CustomerNos { get; set; } = new();
}

/// <summary>
/// Filter model for analytics
/// </summary>
public class AnalyticsFilterModel
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Gender { get; set; }
    public string? CreatedBy { get; set; }
}
