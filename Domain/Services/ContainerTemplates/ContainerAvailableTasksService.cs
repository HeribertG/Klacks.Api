using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.ContainerTemplates;

public class ContainerAvailableTasksService : IContainerAvailableTasksService
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IContainerTemplateRepository _containerTemplateRepository;
    private readonly ILogger<ContainerAvailableTasksService> _logger;

    public ContainerAvailableTasksService(
        IShiftRepository shiftRepository,
        IGroupItemRepository groupItemRepository,
        IContainerTemplateRepository containerTemplateRepository,
        ILogger<ContainerAvailableTasksService> logger)
    {
        _shiftRepository = shiftRepository;
        _groupItemRepository = groupItemRepository;
        _containerTemplateRepository = containerTemplateRepository;
        _logger = logger;
    }

    public async Task<List<Shift>> GetAvailableTasksAsync(
        Guid containerId,
        int weekday,
        TimeOnly fromTime,
        TimeOnly untilTime,
        string? searchString = null,
        Guid? excludeContainerId = null,
        bool? isHoliday = null,
        bool? isWeekdayAndHoliday = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting available tasks for container: {ContainerId}, weekday: {Weekday}, timeframe: {FromTime}-{UntilTime}",
            containerId, weekday, fromTime, untilTime);

        var containerGroupIds = await _groupItemRepository.GetGroupIdsByShiftId(containerId, cancellationToken);
        _logger.LogInformation("Container shift belongs to {Count} groups", containerGroupIds.Count);

        var usedShiftIds = await _containerTemplateRepository.GetUsedShiftIds(excludeContainerId, cancellationToken);
        _logger.LogInformation("Found {Count} shifts already used in containers", usedShiftIds.Count);

        List<Guid>? shiftsInSameGroups = null;
        if (containerGroupIds.Count > 0)
        {
            shiftsInSameGroups = await _groupItemRepository.GetShiftIdsByGroupIds(containerGroupIds, cancellationToken);
            _logger.LogInformation("Found {Count} shifts in container's direct groups. Shifts: [{ShiftIds}]",
                shiftsInSameGroups.Count,
                string.Join(", ", shiftsInSameGroups.Take(5)));

            if (shiftsInSameGroups.Count <= 1)
            {
                _logger.LogInformation("Container's direct group has few/no tasks - loading from ALL groups (no filter)");
                shiftsInSameGroups = null;
            }
        }
        else
        {
            _logger.LogInformation("Container has no groups - skipping group filter");
        }

        var query = BuildAvailableTasksQuery(
            fromTime,
            untilTime,
            weekday,
            usedShiftIds,
            shiftsInSameGroups,
            searchString,
            isHoliday,
            isWeekdayAndHoliday);

        var availableTasks = await query
            .Include(s => s.Client)
                .ThenInclude(c => c.Addresses)
            .AsNoTracking()
            .OrderBy(s => s.StartShift)
            .ThenBy(s => s.Client!.Name)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} available tasks", availableTasks.Count);

        return availableTasks;
    }

    private IQueryable<Shift> BuildAvailableTasksQuery(
        TimeOnly fromTime,
        TimeOnly untilTime,
        int weekday,
        List<Guid> usedShiftIds,
        List<Guid>? shiftsInSameGroups,
        string? searchString,
        bool? isHoliday,
        bool? isWeekdayAndHoliday)
    {
        var timeframeCrossesMidnight = fromTime > untilTime;

        var query = _shiftRepository.GetQuery()
            .Where(s => s.ShiftType == ShiftType.IsTask)
            .Where(s => s.Status >= ShiftStatus.OriginalShift)
            .Where(s => !usedShiftIds.Contains(s.Id))
            .Where(s => !s.IsSporadic);

        if (timeframeCrossesMidnight)
        {
            query = query.Where(s =>
                (s.IsTimeRange && (s.StartShift < untilTime || s.EndShift > fromTime)) ||
                (!s.IsTimeRange && s.StartShift < s.EndShift && (s.StartShift >= fromTime || s.EndShift <= untilTime)));
        }
        else
        {
            query = query.Where(s =>
                (s.IsTimeRange && s.StartShift < untilTime && s.EndShift > fromTime) ||
                (!s.IsTimeRange && s.StartShift >= fromTime && s.EndShift <= untilTime && s.StartShift < s.EndShift));
        }

        if (shiftsInSameGroups != null && shiftsInSameGroups.Count > 0)
        {
            query = query.Where(s => shiftsInSameGroups.Contains(s.Id));
        }
        else
        {
            _logger.LogWarning("No group filter applied - this will return ALL tasks matching time/weekday criteria");
        }

        query = ApplyWeekdayFilter(query, weekday);

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var searchLower = searchString.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(searchLower) ||
                s.Abbreviation.ToLower().Contains(searchLower) ||
                s.Description.ToLower().Contains(searchLower));
        }

        if (isHoliday.HasValue)
        {
            query = query.Where(s => s.IsHoliday == isHoliday.Value);
        }

        if (isWeekdayAndHoliday.HasValue)
        {
            query = query.Where(s => s.IsWeekdayAndHoliday == isWeekdayAndHoliday.Value);
        }

        return query;
    }

    private IQueryable<Shift> ApplyWeekdayFilter(IQueryable<Shift> query, int weekday)
    {
        return weekday switch
        {
            0 => query.Where(s => s.IsSunday),
            1 => query.Where(s => s.IsMonday),
            2 => query.Where(s => s.IsTuesday),
            3 => query.Where(s => s.IsWednesday),
            4 => query.Where(s => s.IsThursday),
            5 => query.Where(s => s.IsFriday),
            6 => query.Where(s => s.IsSaturday),
            _ => query
        };
    }
}
