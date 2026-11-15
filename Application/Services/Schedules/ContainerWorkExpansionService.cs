// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Expands a container work into sub-works, sub-breaks, and work changes based on day-specific overrides or weekly ContainerTemplate items.
/// Before anything is written, every task shift of the chosen override or template must lie in the world of the container work
/// (ShiftWorld): a shift of a variant (key) or of another scenario is refused like a missing shift.
/// Templates and overrides carry no analyse token and are not cloned. A container work of a scenario on a cloned container
/// without an own override or template for the day therefore reads those of the real container (ScenarioSourceShiftId), and
/// each task shift is placed on its clone in that scenario (the real shift when the scenario holds no clone of it). Its
/// sub-works, sub-breaks and work changes carry the token of the container work.
/// </summary>
/// <param name="containerTemplateRepository">Provides templates and items for a container shift</param>
/// <param name="overrideRepository">Provides day-specific overrides that take priority over weekly templates</param>
/// <param name="workRepository">Used to persist newly created sub-works</param>
/// <param name="breakRepository">Used to persist newly created sub-breaks</param>
/// <param name="workChangeRepository">Used to persist briefing, debriefing, and travel time entries</param>
/// <param name="shiftRepository">Used to load and validate the container shift type</param>
/// <param name="childrenManager">Calculates a new sub-break like the recalculation of its container</param>
/// <param name="shiftReader">Finds the clones of the real task shifts in the scenario of the container work</param>
/// <param name="workWriteGuard">Refuses template and override items whose shift lies outside the world of the container work</param>
/// <param name="logger">Logger for diagnostic output</param>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules;

public class ContainerWorkExpansionService : IContainerWorkExpansionService
{
    private const string DescriptionBriefing = "briefing_time";
    private const string DescriptionDebriefing = "debriefing_time";
    private const string DescriptionTravelBefore = "travel_time_before";
    private const string DescriptionTravelAfter = "travel_time_after";
    private const double MinutesPerHour = 60.0;

    private readonly IContainerTemplateRepository _containerTemplateRepository;
    private readonly IContainerShiftOverrideRepository _overrideRepository;
    private readonly IWorkRepository _workRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly IShiftRepository _shiftRepository;
    private readonly IWorkWriteGuard _workWriteGuard;
    private readonly IContainerWorkChildrenManager _childrenManager;
    private readonly IShiftReader _shiftReader;
    private readonly ILogger<ContainerWorkExpansionService> _logger;

    public ContainerWorkExpansionService(
        IContainerTemplateRepository containerTemplateRepository,
        IContainerShiftOverrideRepository overrideRepository,
        IWorkRepository workRepository,
        IBreakRepository breakRepository,
        IWorkChangeRepository workChangeRepository,
        IShiftRepository shiftRepository,
        IWorkWriteGuard workWriteGuard,
        IContainerWorkChildrenManager childrenManager,
        IShiftReader shiftReader,
        ILogger<ContainerWorkExpansionService> logger)
    {
        _containerTemplateRepository = containerTemplateRepository;
        _overrideRepository = overrideRepository;
        _workRepository = workRepository;
        _breakRepository = breakRepository;
        _workChangeRepository = workChangeRepository;
        _shiftRepository = shiftRepository;
        _workWriteGuard = workWriteGuard;
        _childrenManager = childrenManager;
        _shiftReader = shiftReader;
        _logger = logger;
    }

    public async Task ExpandAsync(Work containerWork, DateOnly date)
    {
        var shift = await _shiftRepository.Get(containerWork.ShiftId);

        if (shift is null || shift.ShiftType != ShiftType.IsContainer)
            return;

        var sourceContainerId = containerWork.AnalyseToken.HasValue ? shift.ScenarioSourceShiftId : null;
        var (overrideEntity, overrideFromSource) = await LoadOverrideAsync(containerWork.ShiftId, sourceContainerId, date);

        if (overrideEntity != null)
        {
            var overrideShiftInWorld = await ItemShiftInWorldAsync(
                containerWork, overrideFromSource, overrideEntity.ContainerShiftOverrideItems.Select(item => item.ShiftId));
            await EnsureItemShiftsInWorldAsync(
                containerWork, overrideEntity.ContainerShiftOverrideItems.Select(item => InWorld(item.ShiftId, overrideShiftInWorld)));

            containerWork.StartBase = overrideEntity.StartBase;
            containerWork.EndBase = overrideEntity.EndBase;

            foreach (var item in overrideEntity.ContainerShiftOverrideItems)
            {
                if (item.ShiftId.HasValue)
                {
                    await ExpandShiftItemFromOverride(containerWork, date, item, overrideShiftInWorld(item.ShiftId.Value));
                }
                else if (item.AbsenceId.HasValue)
                {
                    await ExpandAbsenceItemFromOverride(containerWork, date, item);
                }
            }
            return;
        }

        var templates = await _containerTemplateRepository.GetTemplatesForContainer(containerWork.ShiftId);
        var templatesFromSource = false;
        if (sourceContainerId is { } sourceTemplateContainerId && templates.Count == 0)
        {
            templates = await _containerTemplateRepository.GetTemplatesForContainer(sourceTemplateContainerId);
            templatesFromSource = true;
        }

        var template = ContainerTemplateForDate.Pick(templates, date);

        if (template is null)
        {
            _logger.LogDebug(
                "No ContainerTemplate found for containerId={ContainerId} on weekday={Weekday}",
                containerWork.ShiftId, (int)date.DayOfWeek);
            return;
        }

        var templateShiftInWorld = await ItemShiftInWorldAsync(
            containerWork, templatesFromSource, template.ContainerTemplateItems.Select(item => item.ShiftId));
        await EnsureItemShiftsInWorldAsync(
            containerWork, template.ContainerTemplateItems.Select(item => InWorld(item.ShiftId, templateShiftInWorld)));

        containerWork.StartBase = template.StartBase;
        containerWork.EndBase = template.EndBase;

        foreach (var item in template.ContainerTemplateItems)
        {
            if (item.ShiftId.HasValue)
            {
                await ExpandShiftItem(containerWork, date, item, templateShiftInWorld(item.ShiftId.Value));
            }
            else if (item.AbsenceId.HasValue)
            {
                await ExpandAbsenceItem(containerWork, date, item);
            }
        }
    }

    private async Task<(ContainerShiftOverride? Override, bool FromSource)> LoadOverrideAsync(
        Guid containerId, Guid? sourceContainerId, DateOnly date)
    {
        var own = await _overrideRepository.GetByContainerAndDateWithItems(containerId, date);
        if (own != null || sourceContainerId is not { } sourceId)
        {
            return (own, false);
        }

        var source = await _overrideRepository.GetByContainerAndDateWithItems(sourceId, date);
        return (source, source != null);
    }

    private async Task<Func<Guid, Guid>> ItemShiftInWorldAsync(
        Work containerWork, bool itemsOfSourceContainer, IEnumerable<Guid?> itemShiftIds)
    {
        if (!itemsOfSourceContainer || containerWork.AnalyseToken is not { } token)
        {
            return shiftId => shiftId;
        }

        var clones = await _shiftReader.GetScenarioClonesBySourceAsync(
            itemShiftIds.OfType<Guid>().Distinct().ToList(), token, CancellationToken.None);
        return shiftId => clones.TryGetValue(shiftId, out var clone) ? clone : shiftId;
    }

    private static Guid? InWorld(Guid? itemShiftId, Func<Guid, Guid> shiftInWorld)
        => itemShiftId is { } shiftId ? shiftInWorld(shiftId) : null;

    private async Task EnsureItemShiftsInWorldAsync(Work containerWork, IEnumerable<Guid?> itemShiftIds)
    {
        var placements = itemShiftIds
            .Where(shiftId => shiftId.HasValue)
            .Select(shiftId => (shiftId!.Value, containerWork.AnalyseToken))
            .ToList();

        await _workWriteGuard.EnsureShiftsInWorkWorldAsync(placements, CancellationToken.None);
    }

    private async Task ExpandShiftItemFromOverride(Work containerWork, DateOnly date, ContainerShiftOverrideItem item, Guid shiftId)
    {
        var clientId = ContainerChildOwner.Of(item.Shift?.ClientId, containerWork.ClientId);
        var isTimeRange = item.Shift?.IsTimeRange ?? false;

        var startTime = isTimeRange && item.TimeRangeStartItem.HasValue
            ? item.TimeRangeStartItem.Value
            : item.StartItem ?? TimeOnly.MinValue;

        var endTime = isTimeRange && item.TimeRangeEndItem.HasValue
            ? item.TimeRangeEndItem.Value
            : item.EndItem ?? TimeOnly.MinValue;

        var duration = endTime >= startTime
            ? (decimal)(endTime - startTime).TotalHours
            : (decimal)(TimeOnly.MaxValue - startTime + (endTime - TimeOnly.MinValue)).TotalHours;

        var subWork = new Work
        {
            Id = Guid.NewGuid(),
            ParentWorkId = containerWork.Id,
            ShiftId = shiftId,
            ClientId = clientId,
            CurrentDate = date,
            StartTime = startTime,
            EndTime = endTime,
            WorkTime = duration,
            TransportMode = item.TransportMode,
            AnalyseToken = containerWork.AnalyseToken
        };

        await _workRepository.Add(subWork);
        await CreateWorkChangesForOverrideItem(subWork, item, startTime, endTime);
    }

    private async Task ExpandAbsenceItemFromOverride(Work containerWork, DateOnly date, ContainerShiftOverrideItem item)
    {
        var startTime = item.StartItem ?? TimeOnly.MinValue;
        var endTime = item.EndItem ?? TimeOnly.MinValue;

        var subBreak = new Break
        {
            Id = Guid.NewGuid(),
            ParentWorkId = containerWork.Id,
            AbsenceId = item.AbsenceId!.Value,
            ClientId = containerWork.ClientId,
            CurrentDate = date,
            StartTime = startTime,
            EndTime = endTime,
            AnalyseToken = containerWork.AnalyseToken
        };

        await _childrenManager.CalculateNewSubBreakAsync(subBreak, containerWork, CancellationToken.None);
        await _breakRepository.Add(subBreak);
    }

    private async Task CreateWorkChangesForOverrideItem(Work subWork, ContainerShiftOverrideItem item, TimeOnly startTime, TimeOnly endTime)
    {
        if (item.BriefingTime > TimeOnly.MinValue)
        {
            var durationMinutes = item.BriefingTime.Hour * MinutesPerHour + item.BriefingTime.Minute;
            var briefingStart = startTime.AddMinutes(-durationMinutes);

            await _workChangeRepository.Add(new WorkChange
            {
                Id = Guid.NewGuid(),
                WorkId = subWork.Id,
                Type = WorkChangeType.CorrectionStart,
                Description = DescriptionBriefing,
                StartTime = briefingStart,
                EndTime = startTime,
                ChangeTime = (decimal)(durationMinutes / MinutesPerHour),
                Surcharges = 0,
                ToInvoice = WorkChangeDefaults.ToInvoice,
                AnalyseToken = subWork.AnalyseToken
            });
        }

        if (item.DebriefingTime > TimeOnly.MinValue)
        {
            var durationMinutes = item.DebriefingTime.Hour * MinutesPerHour + item.DebriefingTime.Minute;
            var debriefingEnd = endTime.AddMinutes(durationMinutes);

            await _workChangeRepository.Add(new WorkChange
            {
                Id = Guid.NewGuid(),
                WorkId = subWork.Id,
                Type = WorkChangeType.CorrectionEnd,
                Description = DescriptionDebriefing,
                StartTime = endTime,
                EndTime = debriefingEnd,
                ChangeTime = (decimal)(durationMinutes / MinutesPerHour),
                Surcharges = 0,
                ToInvoice = WorkChangeDefaults.ToInvoice,
                AnalyseToken = subWork.AnalyseToken
            });
        }

        if (item.TravelTimeBefore > TimeOnly.MinValue)
        {
            var durationMinutes = item.TravelTimeBefore.Hour * MinutesPerHour + item.TravelTimeBefore.Minute;
            var briefingDuration = item.BriefingTime.Hour * MinutesPerHour + item.BriefingTime.Minute;
            var briefingStart = startTime.AddMinutes(-briefingDuration);
            var travelStart = briefingStart.AddMinutes(-durationMinutes);

            await _workChangeRepository.Add(new WorkChange
            {
                Id = Guid.NewGuid(),
                WorkId = subWork.Id,
                Type = WorkChangeType.CorrectionStart,
                Description = DescriptionTravelBefore,
                StartTime = travelStart,
                EndTime = briefingStart,
                ChangeTime = (decimal)(durationMinutes / MinutesPerHour),
                Surcharges = 0,
                ToInvoice = WorkChangeDefaults.ToInvoice,
                AnalyseToken = subWork.AnalyseToken
            });
        }

        if (item.TravelTimeAfter > TimeOnly.MinValue)
        {
            var durationMinutes = item.TravelTimeAfter.Hour * MinutesPerHour + item.TravelTimeAfter.Minute;
            var debriefingDuration = item.DebriefingTime.Hour * MinutesPerHour + item.DebriefingTime.Minute;
            var debriefingEnd = endTime.AddMinutes(debriefingDuration);
            var travelEnd = debriefingEnd.AddMinutes(durationMinutes);

            await _workChangeRepository.Add(new WorkChange
            {
                Id = Guid.NewGuid(),
                WorkId = subWork.Id,
                Type = WorkChangeType.CorrectionEnd,
                Description = DescriptionTravelAfter,
                StartTime = debriefingEnd,
                EndTime = travelEnd,
                ChangeTime = (decimal)(durationMinutes / MinutesPerHour),
                Surcharges = 0,
                ToInvoice = WorkChangeDefaults.ToInvoice,
                AnalyseToken = subWork.AnalyseToken
            });
        }
    }

    private async Task ExpandShiftItem(Work containerWork, DateOnly date, ContainerTemplateItem item, Guid shiftId)
    {
        var clientId = ContainerChildOwner.Of(item.Shift?.ClientId, containerWork.ClientId);
        var isTimeRange = item.Shift?.IsTimeRange ?? false;

        var startTime = isTimeRange && item.TimeRangeStartItem.HasValue
            ? item.TimeRangeStartItem.Value
            : item.StartItem ?? TimeOnly.MinValue;

        var endTime = isTimeRange && item.TimeRangeEndItem.HasValue
            ? item.TimeRangeEndItem.Value
            : item.EndItem ?? TimeOnly.MinValue;

        var duration = endTime >= startTime
            ? (decimal)(endTime - startTime).TotalHours
            : (decimal)(TimeOnly.MaxValue - startTime + (endTime - TimeOnly.MinValue)).TotalHours;

        var subWork = new Work
        {
            Id = Guid.NewGuid(),
            ParentWorkId = containerWork.Id,
            ShiftId = shiftId,
            ClientId = clientId,
            CurrentDate = date,
            StartTime = startTime,
            EndTime = endTime,
            WorkTime = duration,
            TransportMode = item.TransportMode,
            AnalyseToken = containerWork.AnalyseToken
        };

        await _workRepository.Add(subWork);
        await CreateWorkChangesForItem(subWork, item, startTime, endTime);
    }

    private async Task ExpandAbsenceItem(Work containerWork, DateOnly date, ContainerTemplateItem item)
    {
        var startTime = item.StartItem ?? TimeOnly.MinValue;
        var endTime = item.EndItem ?? TimeOnly.MinValue;

        var subBreak = new Break
        {
            Id = Guid.NewGuid(),
            ParentWorkId = containerWork.Id,
            AbsenceId = item.AbsenceId!.Value,
            ClientId = containerWork.ClientId,
            CurrentDate = date,
            StartTime = startTime,
            EndTime = endTime,
            AnalyseToken = containerWork.AnalyseToken
        };

        await _childrenManager.CalculateNewSubBreakAsync(subBreak, containerWork, CancellationToken.None);
        await _breakRepository.Add(subBreak);
    }

    private async Task CreateWorkChangesForItem(Work subWork, ContainerTemplateItem item, TimeOnly startTime, TimeOnly endTime)
    {
        if (item.BriefingTime > TimeOnly.MinValue)
        {
            var durationMinutes = item.BriefingTime.Hour * MinutesPerHour + item.BriefingTime.Minute;
            var briefingStart = startTime.AddMinutes(-durationMinutes);

            await _workChangeRepository.Add(new WorkChange
            {
                Id = Guid.NewGuid(),
                WorkId = subWork.Id,
                Type = WorkChangeType.CorrectionStart,
                Description = DescriptionBriefing,
                StartTime = briefingStart,
                EndTime = startTime,
                ChangeTime = (decimal)(durationMinutes / MinutesPerHour),
                Surcharges = 0,
                ToInvoice = WorkChangeDefaults.ToInvoice,
                AnalyseToken = subWork.AnalyseToken
            });
        }

        if (item.DebriefingTime > TimeOnly.MinValue)
        {
            var durationMinutes = item.DebriefingTime.Hour * MinutesPerHour + item.DebriefingTime.Minute;
            var debriefingEnd = endTime.AddMinutes(durationMinutes);

            await _workChangeRepository.Add(new WorkChange
            {
                Id = Guid.NewGuid(),
                WorkId = subWork.Id,
                Type = WorkChangeType.CorrectionEnd,
                Description = DescriptionDebriefing,
                StartTime = endTime,
                EndTime = debriefingEnd,
                ChangeTime = (decimal)(durationMinutes / MinutesPerHour),
                Surcharges = 0,
                ToInvoice = WorkChangeDefaults.ToInvoice,
                AnalyseToken = subWork.AnalyseToken
            });
        }

        if (item.TravelTimeBefore > TimeOnly.MinValue)
        {
            var durationMinutes = item.TravelTimeBefore.Hour * MinutesPerHour + item.TravelTimeBefore.Minute;
            var briefingDuration = item.BriefingTime.Hour * MinutesPerHour + item.BriefingTime.Minute;
            var briefingStart = startTime.AddMinutes(-briefingDuration);
            var travelStart = briefingStart.AddMinutes(-durationMinutes);

            await _workChangeRepository.Add(new WorkChange
            {
                Id = Guid.NewGuid(),
                WorkId = subWork.Id,
                Type = WorkChangeType.CorrectionStart,
                Description = DescriptionTravelBefore,
                StartTime = travelStart,
                EndTime = briefingStart,
                ChangeTime = (decimal)(durationMinutes / MinutesPerHour),
                Surcharges = 0,
                ToInvoice = WorkChangeDefaults.ToInvoice,
                AnalyseToken = subWork.AnalyseToken
            });
        }

        if (item.TravelTimeAfter > TimeOnly.MinValue)
        {
            var durationMinutes = item.TravelTimeAfter.Hour * MinutesPerHour + item.TravelTimeAfter.Minute;
            var debriefingDuration = item.DebriefingTime.Hour * MinutesPerHour + item.DebriefingTime.Minute;
            var debriefingEnd = endTime.AddMinutes(debriefingDuration);
            var travelEnd = debriefingEnd.AddMinutes(durationMinutes);

            await _workChangeRepository.Add(new WorkChange
            {
                Id = Guid.NewGuid(),
                WorkId = subWork.Id,
                Type = WorkChangeType.CorrectionEnd,
                Description = DescriptionTravelAfter,
                StartTime = debriefingEnd,
                EndTime = travelEnd,
                ChangeTime = (decimal)(durationMinutes / MinutesPerHour),
                Surcharges = 0,
                ToInvoice = WorkChangeDefaults.ToInvoice,
                AnalyseToken = subWork.AnalyseToken
            });
        }
    }
}
