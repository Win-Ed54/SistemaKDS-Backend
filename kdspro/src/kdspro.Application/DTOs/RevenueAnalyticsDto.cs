namespace kdspro.Application.DTOs;

public class RevenueAnalyticsDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal AverageOrderValueChange { get; set; }
    public int OrderCountChange { get; set; }
    public List<DailyRevenueDto> DailyRevenue { get; set; } = new();
    public List<ProductSalesDto> TopProducts { get; set; } = new();
    public List<HourlyRevenueDto> HourlyBreakdown { get; set; } = new();
    public PeriodComparisonDto PeriodComparison { get; set; } = new();
}

public class DailyRevenueDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class HourlyRevenueDto
{
    public int Hour { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class ProductSalesDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalSales { get; set; }
    public decimal AveragePrice { get; set; }
}

public class PeriodComparisonDto
{
    public string CurrentPeriod { get; set; } = string.Empty;
    public decimal CurrentRevenue { get; set; }
    public int CurrentOrders { get; set; }
    
    public string PreviousPeriod { get; set; } = string.Empty;
    public decimal PreviousRevenue { get; set; }
    public int PreviousOrders { get; set; }
    
    public decimal RevenueChangePercentage { get; set; }
    public decimal OrderChangePercentage { get; set; }
}

public class WeeklyRevenueDto
{
    public int WeekNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<DailyRevenueDto> DailyBreakdown { get; set; } = new();
}

public class MonthlyRevenueDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<WeeklyRevenueDto> WeeklyBreakdown { get; set; } = new();
}
