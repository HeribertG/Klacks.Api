// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing absence entry (Break — vacation, sick day, training day etc.). Only fields
/// supplied as parameters are changed. Loads the entry via GetQuery&lt;BreakResource&gt;, mutates it and
/// saves via PutCommand&lt;BreakResource&gt; so period-hour recalculation kicks in.
/// </summary>
/// <param name="breakId">Required. UUID of the break entry to update.</param>
/// <param name="absenceId">Optional. New UUID of the Absence type (vacation/sick/etc.).</param>
/// <param name="startTime">Optional. New start time HH:mm.</param>
/// <param name="endTime">Optional. New end time HH:mm.</param>
/// <param name="workTime">Optional. New counted hours; counts toward TargetHours but not toward MaxWeeklyHours.</param>
/// <param name="surcharges">Optional. New surcharge value.</param>
/// <param name="information">Optional. New free-text note.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_break")]
public class UpdateBreakSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateBreakSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var breakId = GetRequiredGuid(parameters, "breakId");

        BreakResource breakEntry;
        try
        {
            breakEntry = await _mediator.Send(new GetQuery<BreakResource>(breakId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Break '{breakId}' not found.");
        }

        var changed = new List<string>();

        var absenceIdRaw = GetParameter<string>(parameters, "absenceId");
        if (!string.IsNullOrWhiteSpace(absenceIdRaw))
        {
            if (!Guid.TryParse(absenceIdRaw, out var absenceId))
            {
                return SkillResult.Error($"Invalid absenceId UUID '{absenceIdRaw}'.");
            }
            if (absenceId != breakEntry.AbsenceId)
            {
                breakEntry.AbsenceId = absenceId;
                changed.Add("absenceId");
            }
        }

        var startTimeRaw = GetParameter<string>(parameters, "startTime");
        if (!string.IsNullOrWhiteSpace(startTimeRaw))
        {
            if (!TimeOnly.TryParse(startTimeRaw, out var startTime))
            {
                return SkillResult.Error($"Invalid startTime '{startTimeRaw}'. Expected HH:mm.");
            }
            if (startTime != breakEntry.StartTime)
            {
                breakEntry.StartTime = startTime;
                changed.Add("startTime");
            }
        }

        var endTimeRaw = GetParameter<string>(parameters, "endTime");
        if (!string.IsNullOrWhiteSpace(endTimeRaw))
        {
            if (!TimeOnly.TryParse(endTimeRaw, out var endTime))
            {
                return SkillResult.Error($"Invalid endTime '{endTimeRaw}'. Expected HH:mm.");
            }
            if (endTime != breakEntry.EndTime)
            {
                breakEntry.EndTime = endTime;
                changed.Add("endTime");
            }
        }

        var workTime = GetParameter<decimal?>(parameters, "workTime");
        if (workTime.HasValue && workTime.Value != breakEntry.WorkTime)
        {
            breakEntry.WorkTime = workTime.Value;
            changed.Add("workTime");
        }

        var surcharges = GetParameter<decimal?>(parameters, "surcharges");
        if (surcharges.HasValue && surcharges.Value != breakEntry.Surcharges)
        {
            breakEntry.Surcharges = surcharges.Value;
            changed.Add("surcharges");
        }

        var information = GetParameter<string>(parameters, "information");
        if (information != null && information != breakEntry.Information)
        {
            breakEntry.Information = information;
            changed.Add("information");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { BreakId = breakId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — break left unchanged.");
        }

        var updated = await _mediator.Send(new PutCommand<BreakResource>(breakEntry), cancellationToken);
        if (updated == null)
        {
            return SkillResult.Error($"Updating break '{breakId}' failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                BreakId = breakId,
                ChangedFields = changed,
                updated.ClientId,
                updated.AbsenceId,
                Date = updated.CurrentDate,
                updated.StartTime,
                updated.EndTime,
                updated.WorkTime
            },
            $"Break updated ({string.Join(", ", changed)}). Counts toward TargetHours.");
    }
}
