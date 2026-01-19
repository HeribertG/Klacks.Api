using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.ShiftSchedule;
using Klacks.Api.Presentation.DTOs.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class ShiftScheduleRepository : IShiftScheduleRepository
{
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly IShiftGroupFilterService _shiftGroupFilterService;
    private readonly IShiftScheduleFilterService _shiftScheduleFilterService;
    private readonly ILogger<ShiftScheduleRepository> _logger;

    public ShiftScheduleRepository(
        IShiftScheduleService shiftScheduleService,
        IShiftGroupFilterService shiftGroupFilterService,
        IShiftScheduleFilterService shiftScheduleFilterService,
        ILogger<ShiftScheduleRepository> logger)
    {
        _shiftScheduleService = shiftScheduleService;
        _shiftGroupFilterService = shiftGroupFilterService;
        _shiftScheduleFilterService = shiftScheduleFilterService;
        _logger = logger;
    }

    public async Task<(List<ShiftDayAssignment> Shifts, int TotalCount)> GetShiftScheduleAsync(
        ShiftScheduleFilter filter,
        CancellationToken cancellationToken)
    {
        var startDate = filter.StartDate;
        var endDate = filter.EndDate;

        if (startDate == DateOnly.MinValue || endDate == DateOnly.MinValue)
        {
            _logger.LogWarning("Invalid dates received - using current month as fallback");
            var now = DateTime.UtcNow;
            startDate = new DateOnly(now.Year, now.Month, 1).AddDays(-10);
            endDate = new DateOnly(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)).AddDays(10);
        }

        _logger.LogInformation(
            "Fetching shift schedule for {StartDate} to {EndDate}",
            startDate,
            endDate);

        var holidayDates = filter.HolidayDates?
            .Select(d => DateOnly.FromDateTime(d))
            .ToList();

        var visibleGroupIds = await _shiftGroupFilterService.GetVisibleGroupIdsAsync(filter.SelectedGroup);

        var query = _shiftScheduleService.GetShiftScheduleQuery(
            startDate,
            endDate,
            holidayDates,
            visibleGroupIds,
            filter.ShowUngroupedShifts);

        query = _shiftScheduleFilterService.ApplyAllFilters(query, filter);

        var uniqueShiftIds = await query
            .GroupBy(s => s.ShiftId)
            .Select(g => new { ShiftId = g.Key, ShiftName = g.First().ShiftName })
            .OrderBy(x => x.ShiftName)
            .Select(x => x.ShiftId)
            .ToListAsync(cancellationToken);

        var totalCount = uniqueShiftIds.Count;

        var paginatedShiftIds = uniqueShiftIds
            .Skip(filter.StartRow)
            .Take(filter.RowCount)
            .ToHashSet();

        var result = await query
            .Where(s => paginatedShiftIds.Contains(s.ShiftId))
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} shift schedule entries for {ShiftCount} of {TotalCount} shifts",
            result.Count,
            paginatedShiftIds.Count,
            totalCount);

        return (result, totalCount);
    }

    public async Task<List<ShiftDayAssignment>> GetShiftSchedulePartialAsync(
        ShiftSchedulePartialFilter filter,
        CancellationToken cancellationToken)
    {
        if (filter.ShiftDatePairs.Count == 0)
        {
            return [];
        }

        _logger.LogInformation(
            "Fetching partial shift schedule for {Count} shift/date pairs",
            filter.ShiftDatePairs.Count);

        var shiftDatePairs = filter.ShiftDatePairs
            .Select(p => (p.ShiftId, DateOnly.FromDateTime(p.Date)))
            .ToList();

        return await _shiftScheduleService.GetShiftSchedulePartialAsync(
            shiftDatePairs,
            cancellationToken);
    }
}
