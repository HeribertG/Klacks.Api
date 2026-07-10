// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Removes a task shift item from a container's weekday template. Presence is checked before the
/// container edit lock is taken, so a no-op request never acquires a lock. On removal, the container's
/// complete template list (all weekdays, all holiday variants) is submitted — a partial list would delete
/// every other weekday's template.
/// </summary>
/// <param name="containerId">Required. UUID of the container shift (ShiftType.IsContainer).</param>
/// <param name="weekday">Required. ISO weekday 1=Monday..7=Sunday of the template to remove the task from.</param>
/// <param name="taskShiftId">Required. UUID of the task shift item to remove.</param>

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

[SkillImplementation("remove_container_template_task")]
public class RemoveContainerTemplateTaskSkill : BaseSkillImplementation
{
    private const string LockResourceType = "ContainerTemplate";
    private const int MinIsoWeekday = 1;
    private const int MaxIsoWeekday = 7;
    private const int IsoSunday = 7;
    private const int StorageSunday = 0;

    private readonly IShiftRepository _shiftRepository;
    private readonly IMediator _mediator;
    private readonly IUserService _userService;

    public RemoveContainerTemplateTaskSkill(
        IShiftRepository shiftRepository,
        IMediator mediator,
        IUserService userService)
    {
        _shiftRepository = shiftRepository;
        _mediator = mediator;
        _userService = userService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var containerId = GetRequiredGuid(parameters, "containerId");
        var isoWeekday = GetRequiredInt(parameters, "weekday");
        var taskShiftId = GetRequiredGuid(parameters, "taskShiftId");

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

        var precheckTemplates = await _mediator.Send(new GetContainerTemplatesQuery(containerId), cancellationToken);
        var precheckTemplate = SelectTemplateForWeekday(precheckTemplates, storageWeekday);
        if (precheckTemplate is null)
        {
            return SkillResult.Error("No container template configured for this weekday yet — there is nothing to remove.");
        }

        var precheckItem = precheckTemplate.ContainerTemplateItems.FirstOrDefault(i => i.ShiftId == taskShiftId);
        if (precheckItem is null)
        {
            var currentItems = precheckTemplate.ContainerTemplateItems
                .Select(i => i.ShiftId?.ToString() ?? i.AbsenceId?.ToString() ?? "unknown")
                .ToList();
            var currentItemsText = currentItems.Count > 0 ? string.Join(", ", currentItems) : "none";
            return SkillResult.Error(
                $"Task shift {taskShiftId} is not present in the container template for this weekday. " +
                $"Current items: {currentItemsText}.");
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
            var existingItem = targetTemplate?.ContainerTemplateItems.FirstOrDefault(i => i.ShiftId == taskShiftId);
            if (targetTemplate is null || existingItem is null)
            {
                return SkillResult.Error(
                    $"Task shift {taskShiftId} is no longer present in the container template for this weekday.");
            }

            var items = new List<ContainerTemplateItemResource>(targetTemplate.ContainerTemplateItems);
            items.Remove(existingItem);
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

            var stillPresent = updatedTemplate?.ContainerTemplateItems.Any(i => i.ShiftId == taskShiftId) ?? false;
            if (stillPresent)
            {
                return SkillResult.Error(
                    $"Verification failed: task shift {taskShiftId} still appears in the container template after the removal.");
            }

            return SkillResult.SuccessResult(
                new
                {
                    ContainerId = containerId,
                    Weekday = isoWeekday,
                    TaskShiftId = taskShiftId,
                    Verified = true
                },
                $"Task removed from the container template for weekday {isoWeekday} and confirmed in the database (verified).");
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
