using kdspro.Application.DTOs;
using kdspro.Application.Interfaces;
using kdspro.Domain.Entities;
using kdspro.Domain.Enums;
using MongoDB.Driver;

namespace kdspro.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IMongoCollection<Order> _ordersCollection;

    public AnalyticsService(IMongoCollection<Order> ordersCollection)
    {
        _ordersCollection = ordersCollection;
    }

    public async Task<RevenueAnalyticsDto> GetDailyAnalytics(DateTime date)
    {
        var startDate = date.Date;
        var endDate = startDate.AddDays(1);

        return await GetAnalyticsForDateRange(startDate, endDate);
    }

    public async Task<WeeklyRevenueDto> GetWeeklyAnalytics(int weekNumber, int year)
    {
        var jan4 = new DateTime(year, 1, 4);
        var daysToMonday = jan4.DayOfWeek - DayOfWeek.Monday;
        var firstMonday = jan4.AddDays(-daysToMonday);
        var weekStart = firstMonday.AddDays(7 * (weekNumber - 1));
        var weekEnd = weekStart.AddDays(7);

        return await GetWeekAnalyticsInternal(weekNumber, weekStart, weekEnd);
    }

    public async Task<MonthlyRevenueDto> GetMonthlyAnalytics(int month, int year)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        return await GetMonthAnalyticsInternal(month, year, startDate, endDate);
    }

    public async Task<RevenueAnalyticsDto> GetWeekAnalytics(DateTime startDate, DateTime endDate)
    {
        return await GetAnalyticsForDateRange(startDate.Date, endDate.Date.AddDays(1));
    }

    public async Task<RevenueAnalyticsDto> GetMonthAnalytics(DateTime startDate, DateTime endDate)
    {
        return await GetAnalyticsForDateRange(startDate.Date, endDate.Date.AddDays(1));
    }

    public async Task<RevenueAnalyticsDto> GetCustomRangeAnalytics(DateTime startDate, DateTime endDate)
    {
        return await GetAnalyticsForDateRange(startDate.Date, endDate.Date.AddDays(1));
    }

    public async Task<List<DailyRevenueDto>> GetLast30DaysAnalytics()
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-30);

        var orders = await _ordersCollection
            .Find(o => o.CreatedAt >= startDate && o.CreatedAt < endDate.AddDays(1) && o.Status == OrderStatus.Delivered)
            .ToListAsync();

        var groupedByDate = orders
            .GroupBy(o => o.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyRevenueDto
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count(),
                AverageOrderValue = g.Count() > 0 ? g.Sum(o => o.TotalAmount) / g.Count() : 0
            })
            .ToList();

        return groupedByDate;
    }

    private async Task<RevenueAnalyticsDto> GetAnalyticsForDateRange(DateTime startDate, DateTime endDate)
    {
        var orders = await _ordersCollection
            .Find(o => o.CreatedAt >= startDate && o.CreatedAt < endDate && o.Status == OrderStatus.Delivered)
            .ToListAsync();

        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var totalOrders = orders.Count;
        var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        // Obtener período anterior para comparación
        var periodLength = endDate - startDate;
        var previousStartDate = startDate.Add(-periodLength);
        var previousEndDate = startDate;

        var previousOrders = await _ordersCollection
            .Find(o => o.CreatedAt >= previousStartDate && o.CreatedAt < previousEndDate && o.Status == OrderStatus.Delivered)
            .ToListAsync();

        var previousRevenue = previousOrders.Sum(o => o.TotalAmount);
        var previousOrderCount = previousOrders.Count;
        var previousAverageOrderValue = previousOrderCount > 0 ? previousRevenue / previousOrderCount : 0;

        // Desglose por día
        var dailyRevenue = orders
            .GroupBy(o => o.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyRevenueDto
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count(),
                AverageOrderValue = g.Count() > 0 ? g.Sum(o => o.TotalAmount) / g.Count() : 0
            })
            .ToList();

        // Desglose por hora
        var hourlyRevenue = orders
            .GroupBy(o => o.CreatedAt.Hour)
            .OrderBy(g => g.Key)
            .Select(g => new HourlyRevenueDto
            {
                Hour = g.Key,
                Revenue = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count()
            })
            .ToList();

        // Productos más vendidos
        var topProducts = orders
            .SelectMany(o => o.Items.Select(item => new { item.ProductId, item.ProductName, item.Quantity, item.UnitPrice }))
            .GroupBy(x => new { x.ProductId, x.ProductName })
            .Select(g => new ProductSalesDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                QuantitySold = g.Sum(x => x.Quantity),
                TotalSales = g.Sum(x => x.Quantity * x.UnitPrice),
                AveragePrice = g.Average(x => x.UnitPrice)
            })
            .OrderByDescending(p => p.TotalSales)
            .Take(10)
            .ToList();

        var revenueChangePercentage = previousRevenue > 0
            ? ((totalRevenue - previousRevenue) / previousRevenue) * 100
            : (totalRevenue > 0 ? 100 : 0);

        var orderChangePercentage = previousOrderCount > 0
            ? ((totalOrders - previousOrderCount) / (decimal)previousOrderCount) * 100
            : (totalOrders > 0 ? 100 : 0);

        return new RevenueAnalyticsDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            AverageOrderValue = averageOrderValue,
            AverageOrderValueChange = averageOrderValue - previousAverageOrderValue,
            OrderCountChange = totalOrders - previousOrderCount,
            DailyRevenue = dailyRevenue,
            TopProducts = topProducts,
            HourlyBreakdown = hourlyRevenue,
            PeriodComparison = new PeriodComparisonDto
            {
                CurrentPeriod = $"{startDate:dd/MM/yyyy} - {endDate.AddDays(-1):dd/MM/yyyy}",
                CurrentRevenue = totalRevenue,
                CurrentOrders = totalOrders,
                PreviousPeriod = $"{previousStartDate:dd/MM/yyyy} - {previousEndDate.AddDays(-1):dd/MM/yyyy}",
                PreviousRevenue = previousRevenue,
                PreviousOrders = previousOrderCount,
                RevenueChangePercentage = revenueChangePercentage,
                OrderChangePercentage = orderChangePercentage
            }
        };
    }

    private async Task<WeeklyRevenueDto> GetWeekAnalyticsInternal(int weekNumber, DateTime weekStart, DateTime weekEnd)
    {
        var analytics = await GetAnalyticsForDateRange(weekStart, weekEnd);

        return new WeeklyRevenueDto
        {
            WeekNumber = weekNumber,
            StartDate = weekStart,
            EndDate = weekEnd.AddDays(-1),
            TotalRevenue = analytics.TotalRevenue,
            TotalOrders = analytics.TotalOrders,
            AverageOrderValue = analytics.AverageOrderValue,
            DailyBreakdown = analytics.DailyRevenue
        };
    }

    private async Task<MonthlyRevenueDto> GetMonthAnalyticsInternal(int month, int year, DateTime startDate, DateTime endDate)
    {
        var analytics = await GetAnalyticsForDateRange(startDate, endDate);

        // Calcular semanas del mes
        var weeklyBreakdown = new List<WeeklyRevenueDto>();
        var currentWeekStart = startDate;
        var weekNumber = 1;

        while (currentWeekStart < endDate)
        {
            var weekEnd = currentWeekStart.AddDays(7);
            if (weekEnd > endDate) weekEnd = endDate;

            var weekAnalytics = await GetAnalyticsForDateRange(currentWeekStart, weekEnd);

            weeklyBreakdown.Add(new WeeklyRevenueDto
            {
                WeekNumber = weekNumber,
                StartDate = currentWeekStart,
                EndDate = weekEnd.AddDays(-1),
                TotalRevenue = weekAnalytics.TotalRevenue,
                TotalOrders = weekAnalytics.TotalOrders,
                AverageOrderValue = weekAnalytics.AverageOrderValue,
                DailyBreakdown = weekAnalytics.DailyRevenue
            });

            currentWeekStart = weekEnd;
            weekNumber++;
        }

        return new MonthlyRevenueDto
        {
            Month = month,
            Year = year,
            TotalRevenue = analytics.TotalRevenue,
            TotalOrders = analytics.TotalOrders,
            AverageOrderValue = analytics.AverageOrderValue,
            WeeklyBreakdown = weeklyBreakdown
        };
    }
}
