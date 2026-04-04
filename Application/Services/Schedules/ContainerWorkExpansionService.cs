// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Expands a container work into sub-works and sub-breaks based on ContainerTemplate items.
/// </summary>
/// <param name="containerTemplateRepository">Provides templates and items for a container shift</param>
/// <param name="workRepository">Used to persist newly created sub-works</param>
/// <param name="breakRepository">Used to persist newly created sub-breaks</param>
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
    private readonly IContainerTemplateRepository _containerTemplateRepository;
    private readonly IWorkRepository _workRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IShiftRepository _shiftRepository;
    private readonly ILogger<ContainerWorkExpansionService> _logger;

    public ContainerWorkExpansionService(
        IContainerTemplateRepository containerTemplateRepository,
        IWorkRepository workRepository,
        IBreakRepository breakRepository,
        IShiftRepository shiftRepository,
        ILogger<ContainerWorkExpansionService> logger)
    {
        _containerTemplateRepository = containerTemplateRepository;
        _workRepository = workRepository;
        _breakRepository = breakRepository;
        _shiftRepository = shiftRepository;
        _logger = logger;
    }

    public async Task ExpandAsync(Work containerWork, DateOnly date)
    {
        var shift = await _shiftRepository.Get(containerWork.ShiftId);

        if (shift is null || shift.ShiftType != ShiftType.IsContainer)
            return;

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

        foreach (var item in template.ContainerTemplateItems)
        {
            if (item.ShiftId.HasValue)
            {
                var clientId = item.Shift?.ClientId ?? containerWork.ClientId;
                var startTime = item.StartItem ?? TimeOnly.MinValue;
                var endTime = item.EndItem ?? TimeOnly.MinValue;
                var duration = endTime >= startTime
                    ? (decimal)(endTime - startTime).TotalHours
                    : (decimal)(TimeOnly.MaxValue - startTime + (endTime - TimeOnly.MinValue)).TotalHours;

                var subWork = new Work
                {
                    Id = Guid.NewGuid(),
                    ParentWorkId = containerWork.Id,
                    ShiftId = item.ShiftId.Value,
                    ClientId = clientId,
                    CurrentDate = date,
                    StartTime = startTime,
                    EndTime = endTime,
                    WorkTime = duration
                };

                await _workRepository.Add(subWork);
            }
            else if (item.AbsenceId.HasValue)
            {
                var startTime = item.StartItem ?? TimeOnly.MinValue;
                var endTime = item.EndItem ?? TimeOnly.MinValue;

                var subBreak = new Break
                {
                    Id = Guid.NewGuid(),
                    ParentWorkId = containerWork.Id,
                    AbsenceId = item.AbsenceId.Value,
                    ClientId = containerWork.ClientId,
                    CurrentDate = date,
                    StartTime = startTime,
                    EndTime = endTime
                };

                await _breakRepository.Add(subBreak);
            }
        }
    }
}
