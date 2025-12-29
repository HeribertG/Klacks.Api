using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.WorkSchedule;

public class WorkScheduleService : IWorkScheduleService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<WorkScheduleService> _logger;

    public WorkScheduleService(
        DataBaseContext context,
        ILogger<WorkScheduleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IQueryable<ScheduleCell> GetWorkScheduleQuery(
        DateOnly startDate,
        DateOnly endDate,
        List<Guid>? visibleGroupIds = null)
    {
        _logger.LogDebug(
            "Building work schedule query from {StartDate} to {EndDate}, VisibleGroups: {VisibleGroupCount}",
            startDate,
            endDate,
            visibleGroupIds?.Count ?? 0);

        var startDateTime = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var endDateTime = DateTime.SpecifyKind(endDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var visibleGroupArray = visibleGroupIds?.ToArray() ?? [];

        return _context.ScheduleCells
            .FromSqlInterpolated($@"
                SELECT * FROM get_work_schedule(
                    {startDateTime}::DATE,
                    {endDateTime}::DATE,
                    {visibleGroupArray}::UUID[]
                )");
    }
}
