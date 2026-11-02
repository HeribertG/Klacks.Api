// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns the lead and trail of a planned work row into the briefing and debriefing work changes the schedule timeline
/// understands, so the pre-commit checks see the occupied time around a not-yet-written work (a TimeOnly cannot say
/// that the lead starts before midnight).
/// </summary>
/// <param name="row">The planned row whose lead and trail are mapped</param>
/// <param name="workId">Id of the synthetic work the work changes belong to</param>
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public static class PlannedWorkRowPadding
{
    public static IReadOnlyList<WorkChange> ToWorkChanges(PlannedWorkRow row, Guid workId)
    {
        var changes = new List<WorkChange>();
        if (row.LeadHours > 0m)
        {
            changes.Add(Padding(workId, WorkChangeType.Briefing, row.LeadHours));
        }
        if (row.TrailHours > 0m)
        {
            changes.Add(Padding(workId, WorkChangeType.Debriefing, row.TrailHours));
        }
        return changes;
    }

    private static WorkChange Padding(Guid workId, WorkChangeType type, decimal hours) => new()
    {
        Id = Guid.NewGuid(),
        WorkId = workId,
        Type = type,
        ChangeTime = hours,
    };
}
