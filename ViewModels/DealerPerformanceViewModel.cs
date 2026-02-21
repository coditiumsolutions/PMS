namespace PMS.Web.ViewModels;

/// <summary>
/// View model for Dealer Performance Dashboard
/// </summary>
public class DealerPerformanceViewModel
{
    public DealerPerformanceKPIs KPIs { get; set; } = new();
    public List<DealerPerformanceItem> DealerStats { get; set; } = new();
}

public class DealerPerformanceKPIs
{
    public int TotalDealers { get; set; }
    public int ActiveDealers { get; set; }
    public int TotalCustomersAcquired { get; set; }
    public decimal TotalRevenueFromDealers { get; set; }
    public decimal AverageRevenuePerDealer { get; set; }
    public int DealersWithCustomers { get; set; }
}

public class DealerPerformanceItem
{
    public int DealerID { get; set; }
    public string DealershipName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int CustomersAcquired { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<PlanUsageItem> PlanUsage { get; set; } = new();
    public int PaymentCount { get; set; }
}

public class PlanUsageItem
{
    public string PlanNo { get; set; } = string.Empty;
    public int CustomerCount { get; set; }
    public decimal Revenue { get; set; }
}
