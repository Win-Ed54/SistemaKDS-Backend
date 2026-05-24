using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using kdspro.Application.DTOs;
using kdspro.Application.Interfaces;

namespace kdspro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("daily")]
    public async Task<ActionResult<RevenueAnalyticsDto>> GetDailyAnalytics([FromQuery] string? date = null)
    {
        try
        {
            var parsedDate = string.IsNullOrEmpty(date) 
                ? DateTime.UtcNow 
                : DateTime.ParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            var analytics = await _analyticsService.GetDailyAnalytics(parsedDate);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("week")]
    public async Task<ActionResult<RevenueAnalyticsDto>> GetWeekAnalytics([FromQuery] string? startDate = null, [FromQuery] string? endDate = null)
    {
        try
        {
            var start = string.IsNullOrEmpty(startDate) 
                ? DateTime.UtcNow.AddDays(-7).Date 
                : DateTime.ParseExact(startDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            var end = string.IsNullOrEmpty(endDate) 
                ? DateTime.UtcNow.Date 
                : DateTime.ParseExact(endDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            var analytics = await _analyticsService.GetWeekAnalytics(start, end);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("month")]
    public async Task<ActionResult<MonthlyRevenueDto>> GetMonthAnalytics([FromQuery] int? month = null, [FromQuery] int? year = null)
    {
        try
        {
            var now = DateTime.UtcNow;
            var selectedMonth = month ?? now.Month;
            var selectedYear = year ?? now.Year;

            var analytics = await _analyticsService.GetMonthlyAnalytics(selectedMonth, selectedYear);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("month-range")]
    public async Task<ActionResult<RevenueAnalyticsDto>> GetMonthRangeAnalytics([FromQuery] string? startDate = null, [FromQuery] string? endDate = null)
    {
        try
        {
            var start = string.IsNullOrEmpty(startDate) 
                ? DateTime.UtcNow.AddMonths(-1).Date 
                : DateTime.ParseExact(startDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            var end = string.IsNullOrEmpty(endDate) 
                ? DateTime.UtcNow.Date 
                : DateTime.ParseExact(endDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            var analytics = await _analyticsService.GetMonthAnalytics(start, end);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("range")]
    public async Task<ActionResult<RevenueAnalyticsDto>> GetCustomRangeAnalytics([FromQuery] string startDate, [FromQuery] string endDate)
    {
        try
        {
            var start = DateTime.ParseExact(startDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var end = DateTime.ParseExact(endDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            if (start > end)
            {
                return BadRequest(new { error = "La fecha de inicio no puede ser posterior a la fecha de fin." });
            }

            var analytics = await _analyticsService.GetCustomRangeAnalytics(start, end);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("last-30-days")]
    public async Task<ActionResult<List<DailyRevenueDto>>> GetLast30DaysAnalytics()
    {
        try
        {
            var analytics = await _analyticsService.GetLast30DaysAnalytics();
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
