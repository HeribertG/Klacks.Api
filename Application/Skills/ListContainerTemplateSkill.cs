// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the ContainerTemplate rows (and their items) configured for a container shift, optionally
/// filtered to one ISO weekday. Read-only, no container edit lock is acquired. Item order within a
/// template has no backing position column in the database and is returned as-is, never reordered.
/// </summary>
/// <param name="containerId">Required. UUID of the container shift (ShiftType.IsContainer).</param>
/// <param name="weekday">Optional. ISO weekday 1=Monday..7=Sunday; omitted lists all weekdays.</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries.ContainerTemplates;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_container_template")]
public class ListContainerTemplateSkill : BaseSkillImplementation
{
    private const int MinIsoWeekday = 1;
    private const int MaxIsoWeekday = 7;
    private const int IsoSunday = 7;
    private const int StorageSunday = 0;

    private readonly IShiftRepository _shiftRepository;
    private readonly IMediator _mediator;

    public ListContainerTemplateSkill(IShiftRepository shiftRepository, IMediator mediator)
    {
        _shiftRepository = shiftRepository;
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var containerId = GetRequiredGuid(parameters, "containerId");
        var isoWeekday = GetParameter<int?>(parameters, "weekday");

        var container = await _shiftRepository.Get(containerId);
        if (container is null)
        {
            return SkillResult.Error($"Shift/container not found: {containerId}");
        }

        if (container.ShiftType != ShiftType.IsContainer)
        {
            return SkillResult.Error($"Shift {containerId} is not a container shift (ShiftType must be IsContainer).");
        }

        int? storageWeekday = null;
        if (isoWeekday.HasValue)
        {
            if (isoWeekday.Value < MinIsoWeekday || isoWeekday.Value > MaxIsoWeekday)
            {
                return SkillResult.Error("weekday must be between 1 (Monday) and 7 (Sunday).");
            }

            storageWeekday = isoWeekday.Value == IsoSunday ? StorageSunday : isoWeekday.Value;
        }

        var templates = await _mediator.Send(new GetContainerTemplatesQuery(containerId), cancellationToken);

        var filtered = (storageWeekday.HasValue
                ? templates.Where(t => t.Weekday == storageWeekday.Value)
                : templates)
            .OrderBy(t => t.Weekday)
            .ThenBy(t => t.IsHoliday)
            .ToList();

        var projection = filtered.Select(t => new
        {
            t.Id,
            Weekday = ToIsoWeekday(t.Weekday),
            t.IsHoliday,
            t.IsWeekdayAndHoliday,
            t.FromTime,
            t.UntilTime,
            Items = t.ContainerTemplateItems.Select(ProjectItem).ToList()
        }).ToList();

        var message = projection.Count == 0
            ? "No container template found for the requested container/weekday."
            : $"Found {projection.Count} container template(s).";

        return SkillResult.SuccessResult(projection, message);
    }

    private static int ToIsoWeekday(int storageWeekday)
        => storageWeekday == StorageSunday ? IsoSunday : storageWeekday;

    private static Dictionary<string, object?> ProjectItem(ContainerTemplateItemResource item)
    {
        var displayName = item.Shift?.Name ?? item.Absence?.Name.En ?? item.Absence?.Name.De;

        var data = new Dictionary<string, object?>
        {
            ["ShiftId"] = item.ShiftId,
            ["AbsenceId"] = item.AbsenceId,
            ["Name"] = displayName,
            ["StartItem"] = item.StartItem,
            ["EndItem"] = item.EndItem
        };

        if (item.BriefingTime != default)
        {
            data["BriefingTime"] = item.BriefingTime;
        }

        if (item.DebriefingTime != default)
        {
            data["DebriefingTime"] = item.DebriefingTime;
        }

        if (item.TravelTimeBefore != default)
        {
            data["TravelTimeBefore"] = item.TravelTimeBefore;
        }

        if (item.TravelTimeAfter != default)
        {
            data["TravelTimeAfter"] = item.TravelTimeAfter;
        }

        return data;
    }
}
