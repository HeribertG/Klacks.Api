// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.ContainerTemplates;

/// <summary>
/// The available-task list of a container template slot, also the candidate source of the container autofill: the
/// tasks of the container's group (or of every group when the group has at most one shift) that fit the slot and are
/// valid during the container's validity period (AvailableTaskCandidateCriteria; no period filter when the container
/// cannot be read, e.g. unknown or soft-deleted, as before the filter existed), minus the tasks used in another container, plus the extra task ids an override
/// request names.
/// </summary>
/// <param name="shiftReader">Reads the container's validity period, the candidate tasks and the override tasks</param>
/// <param name="groupItemRepository">Resolves the container's groups and their shifts</param>
/// <param name="containerTemplateRepository">Lists the tasks already used in container templates</param>
/// <param name="logger">Diagnostics of each lookup</param>
public class ContainerAvailableTasksService : IContainerAvailableTasksService
{
    private const int LoggedShiftIdsMax = 5;

    private readonly IShiftReader _shiftReader;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IContainerTemplateRepository _containerTemplateRepository;
    private readonly ILogger<ContainerAvailableTasksService> _logger;

    public ContainerAvailableTasksService(
        IShiftReader shiftReader,
        IGroupItemRepository groupItemRepository,
        IContainerTemplateRepository containerTemplateRepository,
        ILogger<ContainerAvailableTasksService> logger)
    {
        _shiftReader = shiftReader;
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
        IReadOnlyCollection<Guid>? additionalAvailableWorkIds = null,
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
                string.Join(", ", shiftsInSameGroups.Take(LoggedShiftIdsMax)));

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

        if (shiftsInSameGroups == null || shiftsInSameGroups.Count == 0)
        {
            _logger.LogWarning("No group filter applied - this will return ALL tasks matching time/weekday criteria");
        }

        var containerValidity = await _shiftReader.GetValidityPeriodAsync(containerId, cancellationToken);

        var criteria = new AvailableTaskCandidateCriteria(weekday, fromTime, untilTime)
        {
            ValidFrom = containerValidity?.FromDate,
            ValidUntil = containerValidity?.UntilDate,
            IsHoliday = isHoliday,
            IsWeekdayAndHoliday = isWeekdayAndHoliday,
            ExcludedShiftIds = usedShiftIds,
            OnlyShiftIds = shiftsInSameGroups is { Count: > 0 } ? shiftsInSameGroups : null,
            SearchFragment = searchString,
        };

        var availableTasks = (await _shiftReader.GetAvailableTaskCandidatesAsync(criteria, cancellationToken)).ToList();

        _logger.LogInformation("Found {Count} available tasks", availableTasks.Count);

        if (additionalAvailableWorkIds is { Count: > 0 })
        {
            _logger.LogInformation(
                "Override request: {Count} additional work ids provided: [{Ids}]",
                additionalAvailableWorkIds.Count,
                string.Join(", ", additionalAvailableWorkIds).ForLog());

            var overrideIds = additionalAvailableWorkIds
                .Where(id => availableTasks.All(t => t.Id != id))
                .ToList();

            if (overrideIds.Count > 0)
            {
                var overrideShifts = await _shiftReader.GetTasksWithClientByIdsAsync(overrideIds, cancellationToken);

                _logger.LogInformation(
                    "Added {Count} override shifts (hard override): [{Abbrevs}]",
                    overrideShifts.Count,
                    string.Join(", ", overrideShifts.Select(s => s.Abbreviation)));

                availableTasks.AddRange(overrideShifts);
            }
        }

        return availableTasks;
    }
}
