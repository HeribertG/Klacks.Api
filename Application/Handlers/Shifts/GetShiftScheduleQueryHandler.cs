using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Domain.Services.ShiftSchedule;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Shifts;

public class GetShiftScheduleQueryHandler : IRequestHandler<GetShiftScheduleQuery, List<ShiftScheduleResource>>
{
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly ILogger<GetShiftScheduleQueryHandler> _logger;

    public GetShiftScheduleQueryHandler(
        IShiftScheduleService shiftScheduleService,
        ILogger<GetShiftScheduleQueryHandler> logger)
    {
        _shiftScheduleService = shiftScheduleService;
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

        var shiftDayAssignments = await _shiftScheduleService.GetShiftScheduleAsync(
            startDate,
            endDate,
            null,
            cancellationToken);

        var result = shiftDayAssignments.Select(s => new ShiftScheduleResource
        {
            ShiftId = s.ShiftId,
            Date = s.Date,
            DayOfWeek = s.DayOfWeek,
            ShiftName = s.ShiftName
        }).ToList();

        _logger.LogInformation("Retrieved {Count} shift schedule entries", result.Count);

        return result;
    }
}
