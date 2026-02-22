using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Klacks.Api.Infrastructure.Services.ShiftSchedule;

public class ShiftScheduleService : IShiftScheduleService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<ShiftScheduleService> _logger;

    public ShiftScheduleService(
        DataBaseContext context,
        ILogger<ShiftScheduleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IQueryable<ShiftDayAssignment> GetShiftScheduleQuery(
        DateOnly startDate,
        DateOnly endDate,
        List<DateOnly>? holidayDates = null,
        List<Guid>? visibleGroupIds = null,
        bool showUngroupedShifts = false)
    {
        _logger.LogDebug(
            "Building shift schedule query from {StartDate} to {EndDate} with {HolidayCount} holidays, VisibleGroups: {VisibleGroupCount}, ShowUngrouped: {ShowUngrouped}",
            startDate,
            endDate,
            holidayDates?.Count ?? 0,
            visibleGroupIds?.Count ?? 0,
            showUngroupedShifts);

        var holidays = holidayDates ?? [];
        var holidayArray = holidays
            .Select(h => DateTime.SpecifyKind(h.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc))
            .ToArray();

        var startDateTime = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var endDateTime = DateTime.SpecifyKind(endDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var visibleGroupArray = visibleGroupIds?.ToArray() ?? [];

        return _context.ShiftDayAssignments
            .FromSqlInterpolated($@"
                SELECT * FROM get_shift_schedule(
                    {startDateTime}::DATE,
                    {endDateTime}::DATE,
                    {holidayArray}::DATE[],
                    {visibleGroupArray}::UUID[],
                    {showUngroupedShifts}
                )");
    }

    public async Task<List<ShiftDayAssignment>> GetShiftSchedulePartialAsync(
        List<(Guid ShiftId, DateOnly Date)> shiftDatePairs,
        CancellationToken cancellationToken = default)
    {
        if (shiftDatePairs.Count == 0)
        {
            return [];
        }

        _logger.LogDebug(
            "Fetching partial shift schedule for {Count} shift/date pairs",
            shiftDatePairs.Count);

        var pairsArrayString = string.Join(",", shiftDatePairs.Select(p =>
            $"('{p.ShiftId}'::UUID, '{p.Date:yyyy-MM-dd}'::DATE)"));

        var sql = $@"
            SELECT * FROM get_shift_schedule_partial(
                ARRAY[{pairsArrayString}]::shift_date_pair[]
            )";

        return await _context.ShiftDayAssignments
            .FromSqlRaw(sql)
            .ToListAsync(cancellationToken);
    }
}
