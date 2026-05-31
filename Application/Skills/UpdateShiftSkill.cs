// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing shift by PATCHING only the supplied fields onto the current shift (loaded
/// first, so unspecified fields — times, weekdays, groups, expenses — are preserved). Refuses when
/// the shift has cuts (a nested-set subtree) because re-defining a cut parent is not yet supported.
/// Warns when the shift already has works, since changing its times/weekdays re-defines those works.
/// Dispatches PutCommand&lt;ShiftResource&gt;.
/// </summary>
/// <param name="shiftId">Required. UUID of the shift to update.</param>
/// <param name="name">Optional. New name.</param>
/// <param name="abbreviation">Optional. New abbreviation.</param>
/// <param name="description">Optional. New description.</param>
/// <param name="startTime">Optional. New start time HH:mm.</param>
/// <param name="endTime">Optional. New end time HH:mm.</param>
/// <param name="sumEmployees">Optional. New required headcount.</param>
/// <param name="fromDate">Optional. New validity start (ISO yyyy-MM-dd).</param>
/// <param name="untilDate">Optional. New validity end (ISO yyyy-MM-dd).</param>
/// <param name="weekdays">Optional. "all" or a comma list (mon,tue,wed,thu,fri,sat,sun) replacing the weekday flags.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_shift")]
public class UpdateShiftSkill : BaseSkillImplementation
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IMediator _mediator;

    public UpdateShiftSkill(
        IShiftRepository shiftRepository,
        ScheduleMapper scheduleMapper,
        IMediator mediator)
    {
        _shiftRepository = shiftRepository;
        _scheduleMapper = scheduleMapper;
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var shiftId = GetRequiredGuid(parameters, "shiftId");
        var existing = await _shiftRepository.Get(shiftId);
        if (existing == null)
        {
            return SkillResult.Error($"Shift {shiftId} not found.");
        }

        if (existing.Lft.HasValue && existing.Rgt.HasValue && existing.Rgt - existing.Lft > 1)
        {
            return SkillResult.Error("This shift has cuts (a sub-shift tree); updating a cut parent is not yet supported.");
        }

        var resource = _scheduleMapper.ToShiftResource(existing);

        var name = GetParameter<string>(parameters, "name");
        if (!string.IsNullOrWhiteSpace(name)) resource.Name = name.Trim();

        var abbreviation = GetParameter<string>(parameters, "abbreviation");
        if (!string.IsNullOrWhiteSpace(abbreviation)) resource.Abbreviation = abbreviation.Trim();

        var description = GetParameter<string>(parameters, "description");
        if (description != null) resource.Description = description;

        var startRaw = GetParameter<string>(parameters, "startTime");
        if (!string.IsNullOrWhiteSpace(startRaw))
        {
            if (!TimeOnly.TryParse(startRaw, out var start)) return SkillResult.Error($"Invalid startTime: {startRaw}.");
            resource.StartShift = start;
        }

        var endRaw = GetParameter<string>(parameters, "endTime");
        if (!string.IsNullOrWhiteSpace(endRaw))
        {
            if (!TimeOnly.TryParse(endRaw, out var end)) return SkillResult.Error($"Invalid endTime: {endRaw}.");
            resource.EndShift = end;
        }

        var sumEmployees = GetParameter<int?>(parameters, "sumEmployees");
        if (sumEmployees.HasValue && sumEmployees.Value > 0) resource.SumEmployees = sumEmployees.Value;

        var fromDate = GetParameter<DateOnly?>(parameters, "fromDate");
        if (fromDate.HasValue) resource.FromDate = fromDate.Value;

        var untilDate = GetParameter<DateOnly?>(parameters, "untilDate");
        if (untilDate.HasValue) resource.UntilDate = untilDate.Value;

        var weekdays = GetParameter<string>(parameters, "weekdays");
        var weekdaysChanged = !string.IsNullOrWhiteSpace(weekdays);
        if (weekdaysChanged) ApplyWeekdays(resource, weekdays!);

        var timesChanged = !string.IsNullOrWhiteSpace(startRaw) || !string.IsNullOrWhiteSpace(endRaw);

        var result = await _mediator.Send(new PutCommand<ShiftResource>(resource), cancellationToken);
        if (result == null)
        {
            return SkillResult.Error($"Shift {shiftId} could not be updated.");
        }

        var worksNote = string.Empty;
        if ((timesChanged || weekdaysChanged) && await _shiftRepository.HasActiveWorksAsync(shiftId, cancellationToken))
        {
            worksNote = " Note: this shift already has works; their effective times follow the shift definition.";
        }

        return SkillResult.SuccessResult(
            new { result.Id, result.Name, StartShift = result.StartShift.ToString(@"hh\:mm"), EndShift = result.EndShift.ToString(@"hh\:mm") },
            $"Shift '{result.Name}' updated.{worksNote}");
    }

    private static void ApplyWeekdays(ShiftResource resource, string weekdays)
    {
        var all = string.Equals(weekdays.Trim(), "all", StringComparison.OrdinalIgnoreCase);
        var tokens = weekdays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .ToHashSet();

        resource.IsMonday = all || tokens.Contains("mon");
        resource.IsTuesday = all || tokens.Contains("tue");
        resource.IsWednesday = all || tokens.Contains("wed");
        resource.IsThursday = all || tokens.Contains("thu");
        resource.IsFriday = all || tokens.Contains("fri");
        resource.IsSaturday = all || tokens.Contains("sat");
        resource.IsSunday = all || tokens.Contains("sun");
    }
}
