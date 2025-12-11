using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Services.ShiftSchedule;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Shifts;

public class GetShiftScheduleQueryHandler : IRequestHandler<GetShiftScheduleQuery, List<ShiftScheduleResource>>
{
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly IShiftGroupFilterService _shiftGroupFilterService;
    private readonly IShiftScheduleFilterService _shiftScheduleFilterService;
    private readonly ILogger<GetShiftScheduleQueryHandler> _logger;

    public GetShiftScheduleQueryHandler(
        IShiftScheduleService shiftScheduleService,
        IShiftGroupFilterService shiftGroupFilterService,
        IShiftScheduleFilterService shiftScheduleFilterService,
        ILogger<GetShiftScheduleQueryHandler> logger)
    {
        _shiftScheduleService = shiftScheduleService;
        _shiftGroupFilterService = shiftGroupFilterService;
        _shiftScheduleFilterService = shiftScheduleFilterService;
        _logger = logger;
    }

    public async Task<List<ShiftScheduleResource>> Handle(
        GetShiftScheduleQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Fetching shift schedule for {Month}/{Year}",
            request.Filter.CurrentMonth,
            request.Filter.CurrentYear);

        var startDate = new DateOnly(
            request.Filter.CurrentYear,
            request.Filter.CurrentMonth,
            1).AddDays(-request.Filter.DayVisibleBeforeMonth);

        var daysInMonth = DateTime.DaysInMonth(
            request.Filter.CurrentYear,
            request.Filter.CurrentMonth);

        var endDate = new DateOnly(
            request.Filter.CurrentYear,
            request.Filter.CurrentMonth,
            daysInMonth).AddDays(request.Filter.DayVisibleAfterMonth);

        var holidayDates = request.Filter.HolidayDates?
            .Select(d => DateOnly.FromDateTime(d))
            .ToList();

        var visibleGroupIds = await _shiftGroupFilterService.GetVisibleGroupIdsAsync(request.Filter.SelectedGroup);

        var query = _shiftScheduleService.GetShiftScheduleQuery(
            startDate,
            endDate,
            holidayDates,
            request.Filter.SelectedGroup,
            visibleGroupIds);

        query = _shiftScheduleFilterService.ApplyAllFilters(query, request.Filter);

        var shiftDayAssignments = await query.ToListAsync(cancellationToken);

        var result = shiftDayAssignments.Select(s => new ShiftScheduleResource
        {
            ShiftId = s.ShiftId,
            Date = s.Date,
            DayOfWeek = s.DayOfWeek,
            ShiftName = s.ShiftName,
            Abbreviation = s.Abbreviation,
            StartShift = s.StartShift,
            EndShift = s.EndShift,
            WorkTime = s.WorkTime,
            IsSporadic = s.IsSporadic,
            IsTimeRange = s.IsTimeRange,
            ShiftType = s.ShiftType
        }).ToList();

        _logger.LogInformation("Retrieved {Count} shift schedule entries", result.Count);

        return result;
    }
}
