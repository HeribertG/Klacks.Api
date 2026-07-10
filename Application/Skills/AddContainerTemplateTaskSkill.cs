// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Adds a task shift as a new item to a container's weekday template. The task is validated against
/// IContainerAvailableTasksService before the container edit lock is taken, and the container's complete
/// template list (all weekdays, all holiday variants) is submitted on write — a partial list would delete
/// every other weekday's template. Item order within a template has no backing position column, so the
/// requested position is best-effort and only item presence, never its index, is verified after the write.
/// </summary>
/// <param name="containerId">Required. UUID of the container shift (ShiftType.IsContainer).</param>
/// <param name="weekday">Required. ISO weekday 1=Monday..7=Sunday of the template to add the task to.</param>
/// <param name="taskShiftId">Required. UUID of the task shift to add.</param>
/// <param name="position">Optional. 1-based insert position; appended at the end when omitted or out of range.</param>

using Klacks.Api.Application.Commands.ContainerTemplates;
using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries.ContainerTemplates;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("add_container_template_task")]
public class AddContainerTemplateTaskSkill : BaseSkillImplementation
{
    private const string LockResourceType = "ContainerTemplate";
    private const int MinIsoWeekday = 1;
    private const int MaxIsoWeekday = 7;
    private const int IsoSunday = 7;
    private const int StorageSunday = 0;
    private const int MaxSampleTasks = 10;

    private readonly IShiftRepository _shiftRepository;
    private readonly IMediator _mediator;
    private readonly IUserService _userService;
    private readonly IContainerAvailableTasksService _availableTasksService;

    public AddContainerTemplateTaskSkill(
        IShiftRepository shiftRepository,
        IMediator mediator,
        IUserService userService,
        IContainerAvailableTasksService availableTasksService)
    {
        _shiftRepository = shiftRepository;
        _mediator = mediator;
        _userService = userService;
        _availableTasksService = availableTasksService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var containerId = GetRequiredGuid(parameters, "containerId");
        var isoWeekday = GetRequiredInt(parameters, "weekday");
        var taskShiftId = GetRequiredGuid(parameters, "taskShiftId");
        var position = GetParameter<int?>(parameters, "position");

        if (isoWeekday < MinIsoWeekday || isoWeekday > MaxIsoWeekday)
        {
            return SkillResult.Error("weekday must be between 1 (Monday) and 7 (Sunday).");
        }

        var storageWeekday = isoWeekday == IsoSunday ? StorageSunday : isoWeekday;

        var container = await _shiftRepository.Get(containerId);
        if (container is null)
        {
            return SkillResult.Error($"Shift/container not found: {containerId}");
        }

        if (container.ShiftType != ShiftType.IsContainer)
        {
            return SkillResult.Error($"Shift {containerId} is not a container shift (ShiftType must be IsContainer).");
        }

        var taskShift = await _shiftRepository.Get(taskShiftId);
        if (taskShift is null)
        {
            return SkillResult.Error($"Task shift not found: {taskShiftId}");
        }

        var precheckTemplates = await _mediator.Send(new GetContainerTemplatesQuery(containerId), cancellationToken);
        var precheckTemplate = SelectTemplateForWeekday(precheckTemplates, storageWeekday);
        if (precheckTemplate is null)
        {
            return SkillResult.Error(
                "No container template configured for this weekday yet — configure the container template for this weekday first.");
        }

        var availableTasks = await _availableTasksService.GetAvailableTasksAsync(
            containerId,
            storageWeekday,
            precheckTemplate.FromTime,
            precheckTemplate.UntilTime,
            cancellationToken: cancellationToken);

        if (!availableTasks.Any(s => s.Id == taskShiftId))
        {
            var sample = availableTasks.Take(MaxSampleTasks).Select(s => s.Abbreviation).ToList();
            var sampleText = sample.Count > 0 ? string.Join(", ", sample) : "none";
            return SkillResult.Error(
                $"Task shift {taskShiftId} is not currently eligible for this container on this weekday " +
                "(it may be sporadic, the wrong shift type, below the minimum status, outside the " +
                $"template's time window, or already used elsewhere). Currently available tasks: {sampleText}.");
        }

        var instanceId = _userService.GetInstanceId() ?? string.Empty;
        var lockResult = await _mediator.Send(
            new AcquireContainerLockCommand(LockResourceType, containerId, instanceId), cancellationToken);

        if (!lockResult.Acquired)
        {
            return SkillResult.Error("The container is currently being edited by another user.");
        }

        try
        {
            var templates = await _mediator.Send(new GetContainerTemplatesQuery(containerId), cancellationToken);
            var targetTemplate = SelectTemplateForWeekday(templates, storageWeekday);
            if (targetTemplate is null)
            {
                return SkillResult.Error(
                    "No container template configured for this weekday yet — configure the container template for this weekday first.");
            }

            var newItem = new ContainerTemplateItemResource
            {
                Id = Guid.Empty,
                ShiftId = taskShift.Id,
                AbsenceId = null,
                StartItem = taskShift.StartShift,
                EndItem = taskShift.EndShift,
                BriefingTime = taskShift.BriefingTime,
                DebriefingTime = taskShift.DebriefingTime,
                TravelTimeAfter = taskShift.TravelTimeAfter,
                TravelTimeBefore = taskShift.TravelTimeBefore,
                TimeRangeStartItem = null,
                TimeRangeEndItem = null,
                TransportMode = TransportMode.ByCar
            };

            var items = new List<ContainerTemplateItemResource>(targetTemplate.ContainerTemplateItems);
            var insertIndex = items.Count;
            if (position.HasValue && position.Value >= 1 && position.Value <= items.Count + 1)
            {
                insertIndex = position.Value - 1;
            }

            items.Insert(insertIndex, newItem);
            targetTemplate.ContainerTemplateItems = items;

            List<ContainerTemplateResource> putResult;
            try
            {
                putResult = await _mediator.Send(new PutContainerTemplatesCommand(containerId, templates), cancellationToken);
            }
            catch (ContainerLockedException ex)
            {
                return SkillResult.Error(ex.Message);
            }

            var updatedTemplate = putResult.FirstOrDefault(t =>
                t.Weekday == targetTemplate.Weekday
                && t.IsHoliday == targetTemplate.IsHoliday
                && t.IsWeekdayAndHoliday == targetTemplate.IsWeekdayAndHoliday);

            var verified = updatedTemplate?.ContainerTemplateItems.Any(i => i.ShiftId == taskShiftId) ?? false;
            if (!verified)
            {
                return SkillResult.Error(
                    $"Verification failed: task shift {taskShiftId} could not be confirmed in the container template after the write.");
            }

            return SkillResult.SuccessResult(
                new
                {
                    ContainerId = containerId,
                    Weekday = isoWeekday,
                    TaskShiftId = taskShiftId,
                    TaskAbbreviation = taskShift.Abbreviation,
                    Position = insertIndex + 1,
                    Verified = true
                },
                $"Task '{taskShift.Abbreviation}' added to the container template for weekday {isoWeekday} and confirmed in the database (verified).");
        }
        finally
        {
            try
            {
                await _mediator.Send(new ReleaseContainerLockCommand(lockResult.Id), cancellationToken);
            }
            catch (Exception)
            {
            }
        }
    }

    private static ContainerTemplateResource? SelectTemplateForWeekday(List<ContainerTemplateResource> templates, int storageWeekday)
    {
        return templates.FirstOrDefault(t => t.Weekday == storageWeekday && !t.IsHoliday)
            ?? templates.FirstOrDefault(t => t.Weekday == storageWeekday);
    }
}
