// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates a placed work entry in place (the chat equivalent of the schedule's edit-work
/// dialog): start/end time, work time in hours and the free-text note — without deleting
/// and re-creating the work, so its confirmation history stays intact. Locked works
/// (confirmed, approved or in a closed period) are refused with the exact lock level;
/// unconfirm_work or revoke_day_approval first. The update goes through the existing Works
/// PUT pipeline (period-hour recalculation included) and is verified by re-reading the work.
/// </summary>
/// <param name="workId">Required. UUID of the work entry (from read_schedule_state).</param>
/// <param name="startTime">Optional. New start time HH:mm.</param>
/// <param name="endTime">Optional. New end time HH:mm.</param>
/// <param name="workTime">Optional. New work time in hours; when only times change, it is recomputed from them.</param>
/// <param name="information">Optional. New free-text note.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_work")]
public class UpdateWorkSkill : BaseSkillImplementation
{
    private const decimal MinutesPerHour = 60m;
    private const int HoursPerDay = 24;

    private readonly IMediator _mediator;

    public UpdateWorkSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var workId = GetRequiredGuid(parameters, "workId");

        WorkResource? work;
        try
        {
            work = await _mediator.Send(new GetQuery<WorkResource>(workId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            work = null;
        }

        if (work == null)
        {
            return SkillResult.Error($"Work '{workId}' not found.");
        }

        if (work.LockLevel != WorkLockLevel.None)
        {
            return SkillResult.Error(
                $"Work '{workId}' is locked ({work.LockLevel}) and cannot be edited. " +
                "Confirmed works need unconfirm_work first, approved days revoke_day_approval, " +
                "and closed periods reopen_period.");
        }

        var changed = new List<string>();

        var startTimeRaw = GetParameter<string>(parameters, "startTime");
        if (!string.IsNullOrWhiteSpace(startTimeRaw))
        {
            if (!TimeOnly.TryParse(startTimeRaw, out var startTime))
            {
                return SkillResult.Error($"Invalid startTime value: {startTimeRaw}. Expected format HH:mm.");
            }

            if (work.StartTime != startTime)
            {
                work.StartTime = startTime;
                changed.Add("startTime");
            }
        }

        var endTimeRaw = GetParameter<string>(parameters, "endTime");
        if (!string.IsNullOrWhiteSpace(endTimeRaw))
        {
            if (!TimeOnly.TryParse(endTimeRaw, out var endTime))
            {
                return SkillResult.Error($"Invalid endTime value: {endTimeRaw}. Expected format HH:mm.");
            }

            if (work.EndTime != endTime)
            {
                work.EndTime = endTime;
                changed.Add("endTime");
            }
        }

        var workTimeRaw = GetParameter<decimal?>(parameters, "workTime");
        if (workTimeRaw.HasValue)
        {
            if (workTimeRaw.Value <= 0)
            {
                return SkillResult.Error($"workTime must be positive, got {workTimeRaw.Value}.");
            }

            if (work.WorkTime != workTimeRaw.Value)
            {
                work.WorkTime = workTimeRaw.Value;
                changed.Add("workTime");
            }
        }
        else if (changed.Contains("startTime") || changed.Contains("endTime"))
        {
            var recomputed = ComputeWorkTime(work.StartTime, work.EndTime);
            if (work.WorkTime != recomputed)
            {
                work.WorkTime = recomputed;
                changed.Add("workTime");
            }
        }

        var information = GetParameter<string>(parameters, "information");
        if (information != null && information != work.Information)
        {
            work.Information = information;
            changed.Add("information");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { WorkId = workId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — work left unchanged.");
        }

        var updated = await _mediator.Send(new PutCommand<WorkResource>(work), cancellationToken);
        if (updated == null)
        {
            return SkillResult.Error($"Updating work '{workId}' failed.");
        }

        WorkResource? persisted;
        try
        {
            persisted = await _mediator.Send(new GetQuery<WorkResource>(workId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            persisted = null;
        }

        if (persisted == null
            || persisted.StartTime != work.StartTime
            || persisted.EndTime != work.EndTime
            || persisted.WorkTime != work.WorkTime)
        {
            return SkillResult.Error(
                $"Database verification failed: work '{workId}' does not show the new values after the write — " +
                "treat the update as not persisted.");
        }

        return SkillResult.SuccessResult(
            new
            {
                WorkId = workId,
                ChangedFields = changed,
                persisted.ClientId,
                persisted.CurrentDate,
                StartTime = persisted.StartTime.ToString("HH:mm"),
                EndTime = persisted.EndTime.ToString("HH:mm"),
                persisted.WorkTime,
                persisted.Information
            },
            $"Work '{workId}' on {persisted.CurrentDate:yyyy-MM-dd} was updated ({string.Join(", ", changed)}) " +
            $"to {persisted.StartTime:HH\\:mm}–{persisted.EndTime:HH\\:mm} ({persisted.WorkTime}h) and confirmed " +
            "in the database (verified).");
    }

    private static decimal ComputeWorkTime(TimeOnly start, TimeOnly end)
    {
        var minutes = (end.ToTimeSpan() - start.ToTimeSpan()).TotalMinutes;
        if (minutes <= 0)
        {
            minutes += HoursPerDay * (double)MinutesPerHour;
        }

        return Math.Round((decimal)minutes / MinutesPerHour, 2);
    }
}
