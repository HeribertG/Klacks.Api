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

    public async Task<List<ShiftDayAssignment>> GetShiftScheduleAsync(
        ShiftScheduleFilter filter,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Fetching shift schedule for {Month}/{Year}",
            filter.CurrentMonth,
            filter.CurrentYear);

        var startDate = new DateOnly(
            filter.CurrentYear,
            filter.CurrentMonth,
            1).AddDays(-filter.DayVisibleBeforeMonth);

        var daysInMonth = DateTime.DaysInMonth(
            filter.CurrentYear,
            filter.CurrentMonth);

        var endDate = new DateOnly(
            filter.CurrentYear,
            filter.CurrentMonth,
            daysInMonth).AddDays(filter.DayVisibleAfterMonth);

        var holidayDates = filter.HolidayDates?
            .Select(d => DateOnly.FromDateTime(d))
            .ToList();

        var visibleGroupIds = await _shiftGroupFilterService.GetVisibleGroupIdsAsync(filter.SelectedGroup);

        var query = _shiftScheduleService.GetShiftScheduleQuery(
            startDate,
            endDate,
            holidayDates,
            filter.SelectedGroup,
            visibleGroupIds);

        query = _shiftScheduleFilterService.ApplyAllFilters(query, filter);

        var result = await query.ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} shift schedule entries", result.Count);

        return result;
    }
}
