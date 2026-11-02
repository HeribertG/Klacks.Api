// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules.Recovery;

/// <summary>
/// Reads the lead, trail and extra paid hours of every work from the schedule cells' non-replacement work changes
/// (corrections, travel, briefing, debriefing), keyed by the work they belong to.
/// </summary>
public static class RecoveryWorkPaddingReader
{
    /// <summary>
    /// The padding per work id. Replacement work changes are skipped; they hand time to another person instead of
    /// padding the work.
    /// </summary>
    /// <param name="cells">Schedule cells of the context window; a work change cell carries its work in SourceId</param>
    public static IReadOnlyDictionary<Guid, RecoveryWorkPadding> Collect(IReadOnlyList<ScheduleCell> cells)
    {
        var lead = new Dictionary<Guid, decimal>();
        var trail = new Dictionary<Guid, decimal>();
        var startExtra = new Dictionary<Guid, decimal>();
        var endExtra = new Dictionary<Guid, decimal>();
        var within = new Dictionary<Guid, List<(DateTime At, decimal Hours)>>();

        foreach (var cell in cells)
        {
            if (cell.EntryType != (int)ScheduleEntryType.WorkChange
                || cell.IsReplacementEntry
                || cell.WorkChangeType is not { } rawType
                || !CorrectionWorkChangeTypes.IsCorrection((WorkChangeType)rawType))
            {
                continue;
            }

            var type = (WorkChangeType)rawType;
            var hours = cell.ChangeTime ?? 0m;
            var paid = CorrectionWorkChangeTypes.IsHourBearing(type) ? hours : 0m;
            switch (type)
            {
                case WorkChangeType.CorrectionStart or WorkChangeType.Briefing or WorkChangeType.TravelStart:
                    Add(lead, cell.SourceId, hours);
                    Add(startExtra, cell.SourceId, paid);
                    break;
                case WorkChangeType.CorrectionEnd or WorkChangeType.Debriefing or WorkChangeType.TravelEnd:
                    Add(trail, cell.SourceId, hours);
                    Add(endExtra, cell.SourceId, paid);
                    break;
                case WorkChangeType.TravelWithin:
                    AddWithin(within, cell, paid);
                    break;
            }
        }

        var workIds = lead.Keys.Concat(trail.Keys).Concat(startExtra.Keys).Concat(endExtra.Keys).Concat(within.Keys).Distinct();
        return workIds.ToDictionary(
            id => id,
            id => new RecoveryWorkPadding(
                Math.Max(0m, lead.GetValueOrDefault(id)),
                Math.Max(0m, trail.GetValueOrDefault(id)),
                startExtra.GetValueOrDefault(id),
                endExtra.GetValueOrDefault(id),
                within.TryGetValue(id, out var list) ? list : []));
    }

    private static void Add(Dictionary<Guid, decimal> target, Guid workId, decimal hours)
        => target[workId] = target.GetValueOrDefault(workId) + hours;

    private static void AddWithin(Dictionary<Guid, List<(DateTime At, decimal Hours)>> target, ScheduleCell cell, decimal paid)
    {
        if (!target.TryGetValue(cell.SourceId, out var list))
        {
            list = [];
            target[cell.SourceId] = list;
        }
        list.Add((DateOnly.FromDateTime(cell.EntryDate).ToDateTime(TimeOnly.FromTimeSpan(cell.StartTime)), paid));
    }
}
