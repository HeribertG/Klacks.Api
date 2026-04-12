// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Expands a container work into sub-works, sub-breaks, and work changes based on day-specific overrides or weekly ContainerTemplate items.
/// </summary>
/// <param name="containerTemplateRepository">Provides templates and items for a container shift</param>
/// <param name="overrideRepository">Provides day-specific overrides that take priority over weekly templates</param>
/// <param name="workRepository">Used to persist newly created sub-works</param>
/// <param name="breakRepository">Used to persist newly created sub-breaks</param>
/// <param name="workChangeRepository">Used to persist briefing, debriefing, and travel time entries</param>
/// <param name="shiftRepository">Used to load and validate the container shift type</param>
/// <param name="logger">Logger for diagnostic output</param>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
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
    private readonly ILogger<ContainerWorkExpansionService> _logger;

    public ContainerWorkExpansionService(
        IContainerTemplateRepository containerTemplateRepository,
        IContainerShiftOverrideRepository overrideRepository,
        IWorkRepository workRepository,
        IBreakRepository breakRepository,
        IWorkChangeRepository workChangeRepository,
        IShiftRepository shiftRepository,
        ILogger<ContainerWorkExpansionService> logger)
    {
        _containerTemplateRepository = containerTemplateRepository;
        _overrideRepository = overrideRepository;
        _workRepository = workRepository;
        _breakRepository = breakRepository;
        _workChangeRepository = workChangeRepository;
        _shiftRepository = shiftRepository;
        _logger = logger;
    }

    public async Task ExpandAsync(Work containerWork, DateOnly date)
    {
        var shift = await _shiftRepository.Get(containerWork.ShiftId);

        if (shift is null || shift.ShiftType != ShiftType.IsContainer)
            return;

        var overrideEntity = await _overrideRepository.GetByContainerAndDateWithItems(containerWork.ShiftId, date);

        if (overrideEntity != null)
        {
            containerWork.StartBase = overrideEntity.StartBase;
            containerWork.EndBase = overrideEntity.EndBase;

            foreach (var item in overrideEntity.ContainerShiftOverrideItems)
            {
                if (item.ShiftId.HasValue)
                {
                    await ExpandShiftItemFromOverride(containerWork, date, item);
                }
                else if (item.AbsenceId.HasValue)
                {
                    await ExpandAbsenceItemFromOverride(containerWork, date, item);
                }
            }
            return;
        }

        var templates = await _containerTemplateRepository.GetTemplatesForContainer(containerWork.ShiftId);

        var dayOfWeek = (int)date.DayOfWeek;
        var template = templates.FirstOrDefault(t => t.Weekday == dayOfWeek && !t.IsHoliday)
            ?? templates.FirstOrDefault(t => t.Weekday == dayOfWeek);

        if (template is null)
        {
            _logger.LogDebug(
                "No ContainerTemplate found for containerId={ContainerId} on weekday={Weekday}",
                containerWork.ShiftId, dayOfWeek);
            return;
        }

        containerWork.StartBase = template.StartBase;
        containerWork.EndBase = template.EndBase;

        foreach (var item in template.ContainerTemplateItems)
        {
            if (item.ShiftId.HasValue)
            {
                await ExpandShiftItem(containerWork, date, item);
            }
            else if (item.AbsenceId.HasValue)
            {
                await ExpandAbsenceItem(containerWork, date, item);
            }
        }
    }

    private async Task ExpandShiftItemFromOverride(Work containerWork, DateOnly date, ContainerShiftOverrideItem item)
    {
        var clientId = item.Shift?.ClientId ?? containerWork.ClientId;
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
            ShiftId = item.ShiftId!.Value,
            ClientId = clientId,
            CurrentDate = date,
            StartTime = startTime,
            EndTime = endTime,
            WorkTime = duration,
            TransportMode = item.TransportMode
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
            EndTime = endTime
        };

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
                ToInvoice = false
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
                ToInvoice = false
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
                ToInvoice = false
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
                ToInvoice = false
            });
        }
    }

    private async Task ExpandShiftItem(Work containerWork, DateOnly date, ContainerTemplateItem item)
    {
        var clientId = item.Shift?.ClientId ?? containerWork.ClientId;
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
            ShiftId = item.ShiftId!.Value,
            ClientId = clientId,
            CurrentDate = date,
            StartTime = startTime,
            EndTime = endTime,
            WorkTime = duration,
            TransportMode = item.TransportMode
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
            EndTime = endTime
        };

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
                ToInvoice = false
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
                ToInvoice = false
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
                ToInvoice = false
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
                ToInvoice = false
            });
        }
    }
}
