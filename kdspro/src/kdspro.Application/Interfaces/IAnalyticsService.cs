using kdspro.Application.DTOs;

namespace kdspro.Application.Interfaces;

public interface IAnalyticsService
{
    Task<RevenueAnalyticsDto> GetDailyAnalytics(DateTime date);
    Task<WeeklyRevenueDto> GetWeeklyAnalytics(int weekNumber, int year);
    Task<MonthlyRevenueDto> GetMonthlyAnalytics(int month, int year);
    Task<RevenueAnalyticsDto> GetWeekAnalytics(DateTime startDate, DateTime endDate);
    Task<RevenueAnalyticsDto> GetMonthAnalytics(DateTime startDate, DateTime endDate);
    Task<RevenueAnalyticsDto> GetCustomRangeAnalytics(DateTime startDate, DateTime endDate);
    Task<List<DailyRevenueDto>> GetLast30DaysAnalytics();
}
