using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.ShiftSchedule;

public interface IShiftScheduleService
{
    Task<List<ShiftDayAssignment>> GetShiftScheduleAsync(
        DateOnly startDate,
        DateOnly endDate,
        List<DateOnly>? holidayDates = null,
        CancellationToken cancellationToken = default);
}

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

    public async Task<List<ShiftDayAssignment>> GetShiftScheduleAsync(
        DateOnly startDate,
        DateOnly endDate,
        List<DateOnly>? holidayDates = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Getting shift schedule from {StartDate} to {EndDate} with {HolidayCount} holidays",
            startDate,
            endDate,
            holidayDates?.Count ?? 0);

        var holidays = holidayDates ?? [];
        var holidayArray = holidays.Select(h => h.ToDateTime(TimeOnly.MinValue)).ToArray();

        var result = await _context.ShiftDayAssignments
            .FromSqlInterpolated($@"
                SELECT * FROM get_shift_schedule(
                    {startDate.ToDateTime(TimeOnly.MinValue)}::DATE,
                    {endDate.ToDateTime(TimeOnly.MinValue)}::DATE,
                    {holidayArray}::DATE[]
                )")
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Found {Count} shift day assignments", result.Count);

        return result;
    }
}

public static class ShiftScheduleServiceExtensions
{
    public static IServiceCollection AddShiftScheduleService(this IServiceCollection services)
    {
        services.AddScoped<IShiftScheduleService, ShiftScheduleService>();
        return services;
    }
}
