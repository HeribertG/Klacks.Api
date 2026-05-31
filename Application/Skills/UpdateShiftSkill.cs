// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing shift by PATCHING only the supplied fields onto the current shift (loaded
/// first, so unspecified fields — times, weekdays, groups, expenses — are preserved). For a cut
/// parent (a nested-set subtree) structural edits (times / weekdays / dates / headcount) are refused
/// and routed to the cut editor — each cut child defines its own times, so propagating structure
/// would destroy the cuts; only metadata (name / abbreviation / description) is propagated across all
/// members of the cut group. Warns when the shift already has works, since changing its times/weekdays
/// re-defines those works. Dispatches PutCommand&lt;ShiftResource&gt;.
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

        var name = GetParameter<string>(parameters, "name");
        var abbreviation = GetParameter<string>(parameters, "abbreviation");
        var description = GetParameter<string>(parameters, "description");

        var startRaw = GetParameter<string>(parameters, "startTime");
        if (!string.IsNullOrWhiteSpace(startRaw) && !TimeOnly.TryParse(startRaw, out _))
        {
            return SkillResult.Error($"Invalid startTime: {startRaw}.");
        }
        var endRaw = GetParameter<string>(parameters, "endTime");
        if (!string.IsNullOrWhiteSpace(endRaw) && !TimeOnly.TryParse(endRaw, out _))
        {
            return SkillResult.Error($"Invalid endTime: {endRaw}.");
        }
        var sumEmployees = GetParameter<int?>(parameters, "sumEmployees");
        var fromDate = GetParameter<DateOnly?>(parameters, "fromDate");
        var untilDate = GetParameter<DateOnly?>(parameters, "untilDate");
        var weekdays = GetParameter<string>(parameters, "weekdays");

        var metadataChanged = !string.IsNullOrWhiteSpace(name)
            || !string.IsNullOrWhiteSpace(abbreviation)
            || description != null;
        var structuralChanged = !string.IsNullOrWhiteSpace(startRaw)
            || !string.IsNullOrWhiteSpace(endRaw)
            || (sumEmployees.HasValue && sumEmployees.Value > 0)
            || fromDate.HasValue
            || untilDate.HasValue
            || !string.IsNullOrWhiteSpace(weekdays);

        var isCutParent = existing.Lft.HasValue && existing.Rgt.HasValue && existing.Rgt - existing.Lft > 1;

        if (isCutParent)
        {
            if (structuralChanged)
            {
                return SkillResult.Error("This shift has cuts (a sub-shift tree). Structural edits (times / weekdays / dates / headcount) of a cut parent must be made in the cut editor, because each cut defines its own times — only name, abbreviation and description can be changed here.");
            }
            if (!metadataChanged)
            {
                return SkillResult.Error("Provide a name, abbreviation or description to update.");
            }

            return await PropagateMetadataAsync(existing, name, abbreviation, description, cancellationToken);
        }

        var resource = _scheduleMapper.ToShiftResource(existing);
        ApplyMetadata(resource, name, abbreviation, description);

        if (!string.IsNullOrWhiteSpace(startRaw)) resource.StartShift = TimeOnly.Parse(startRaw);
        if (!string.IsNullOrWhiteSpace(endRaw)) resource.EndShift = TimeOnly.Parse(endRaw);
        if (sumEmployees.HasValue && sumEmployees.Value > 0) resource.SumEmployees = sumEmployees.Value;
        if (fromDate.HasValue) resource.FromDate = fromDate.Value;
        if (untilDate.HasValue) resource.UntilDate = untilDate.Value;
        if (!string.IsNullOrWhiteSpace(weekdays)) ApplyWeekdays(resource, weekdays!);

        var result = await _mediator.Send(new PutCommand<ShiftResource>(resource), cancellationToken);
        if (result == null)
        {
            return SkillResult.Error($"Shift {shiftId} could not be updated.");
        }

        var worksNote = string.Empty;
        if (structuralChanged && await _shiftRepository.HasActiveWorksAsync(shiftId, cancellationToken))
        {
            worksNote = " Note: this shift already has works; their effective times follow the shift definition.";
        }

        return SkillResult.SuccessResult(
            new { result.Id, result.Name, StartShift = result.StartShift.ToString(@"hh\:mm"), EndShift = result.EndShift.ToString(@"hh\:mm") },
            $"Shift '{result.Name}' updated.{worksNote}");
    }

    private async Task<SkillResult> PropagateMetadataAsync(
        Domain.Models.Schedules.Shift existing,
        string? name,
        string? abbreviation,
        string? description,
        CancellationToken cancellationToken)
    {
        var groupKey = existing.OriginalId ?? existing.Id;
        var members = await _shiftRepository.CutList(groupKey, tracked: false);
        if (members.All(m => m.Id != existing.Id))
        {
            members.Add(existing);
        }

        foreach (var member in members)
        {
            var memberResource = _scheduleMapper.ToShiftResource(member);
            ApplyMetadata(memberResource, name, abbreviation, description);
            await _mediator.Send(new PutCommand<ShiftResource>(memberResource), cancellationToken);
        }

        var displayName = string.IsNullOrWhiteSpace(name) ? existing.Name : name.Trim();
        return SkillResult.SuccessResult(
            new { existing.Id, Name = displayName, UpdatedMembers = members.Count },
            $"Metadata of '{displayName}' propagated across {members.Count} cut-group member(s).");
    }

    private static void ApplyMetadata(ShiftResource resource, string? name, string? abbreviation, string? description)
    {
        if (!string.IsNullOrWhiteSpace(name)) resource.Name = name.Trim();
        if (!string.IsNullOrWhiteSpace(abbreviation)) resource.Abbreviation = abbreviation.Trim();
        if (description != null) resource.Description = description;
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
