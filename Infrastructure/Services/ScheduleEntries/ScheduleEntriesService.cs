using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.ScheduleEntries;

public class ScheduleEntriesService : IScheduleEntriesService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<ScheduleEntriesService> _logger;

    public ScheduleEntriesService(
        DataBaseContext context,
        ILogger<ScheduleEntriesService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IQueryable<ScheduleCell> GetScheduleEntriesQuery(
        DateOnly startDate,
        DateOnly endDate,
        List<Guid>? visibleGroupIds = null)
    {
        _logger.LogDebug(
            "Building schedule entries query from {StartDate} to {EndDate}, VisibleGroups: {VisibleGroupCount}",
            startDate,
            endDate,
            visibleGroupIds?.Count ?? 0);

        var startDateTime = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var endDateTime = DateTime.SpecifyKind(endDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var visibleGroupArray = visibleGroupIds?.ToArray() ?? [];

        return _context.ScheduleCells
            .FromSqlInterpolated($@"
                SELECT * FROM get_schedule_entries(
                    {startDateTime}::DATE,
                    {endDateTime}::DATE,
                    {visibleGroupArray}::UUID[]
                )");
    }
}
