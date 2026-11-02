// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules.Recovery;

/// <summary>
/// Reads the lead, trail and extra paid hours of works from their non-replacement work changes (corrections, travel,
/// briefing, debriefing), either from schedule cells (keyed by the work they belong to) or from the work changes of one work.
/// </summary>
public static class RecoveryWorkPaddingReader
{
    private readonly record struct Entry(Guid WorkId, WorkChangeType Type, decimal Hours, DateTime At);

    /// <summary>
    /// The padding per work id. Replacement work changes are skipped; they hand time to another person instead of
    /// padding the work.
    /// </summary>
    /// <param name="cells">Schedule cells of the context window; a work change cell carries its work in SourceId</param>
    public static IReadOnlyDictionary<Guid, RecoveryWorkPadding> Collect(IReadOnlyList<ScheduleCell> cells)
        => Sum(cells
            .Where(cell => cell.EntryType == (int)ScheduleEntryType.WorkChange
                && !cell.IsReplacementEntry
                && cell.WorkChangeType is { } raw
                && CorrectionWorkChangeTypes.IsCorrection((WorkChangeType)raw))
            .Select(cell => new Entry(
                cell.SourceId,
                (WorkChangeType)cell.WorkChangeType!.Value,
                cell.ChangeTime ?? 0m,
                DateOnly.FromDateTime(cell.EntryDate).ToDateTime(TimeOnly.FromTimeSpan(cell.StartTime)))));

    /// <summary>
    /// The padding of one work from its stored work changes; replacement changes are skipped.
    /// </summary>
    /// <param name="work">The work the changes belong to</param>
    /// <param name="changes">Work changes of the work</param>
    public static RecoveryWorkPadding Collect(Work work, IEnumerable<WorkChange> changes)
    {
        var workStart = work.CurrentDate.ToDateTime(work.StartTime);
        var entries = changes
            .Where(change => CorrectionWorkChangeTypes.IsCorrection(change.Type))
            .Select(change =>
            {
                var at = work.CurrentDate.ToDateTime(change.StartTime);
                return new Entry(work.Id, change.Type, change.ChangeTime, at < workStart ? at.AddDays(1) : at);
            });
        return Sum(entries).GetValueOrDefault(work.Id, RecoveryWorkPadding.None);
    }

    private static IReadOnlyDictionary<Guid, RecoveryWorkPadding> Sum(IEnumerable<Entry> entries)
    {
        var lead = new Dictionary<Guid, decimal>();
        var trail = new Dictionary<Guid, decimal>();
        var startExtra = new Dictionary<Guid, decimal>();
        var endExtra = new Dictionary<Guid, decimal>();
        var within = new Dictionary<Guid, List<(DateTime At, decimal Hours)>>();

        foreach (var entry in entries)
        {
            var paid = CorrectionWorkChangeTypes.IsHourBearing(entry.Type) ? entry.Hours : 0m;
            switch (entry.Type)
            {
                case WorkChangeType.CorrectionStart or WorkChangeType.Briefing or WorkChangeType.TravelStart:
                    Add(lead, entry.WorkId, entry.Hours);
                    Add(startExtra, entry.WorkId, paid);
                    break;
                case WorkChangeType.CorrectionEnd or WorkChangeType.Debriefing or WorkChangeType.TravelEnd:
                    Add(trail, entry.WorkId, entry.Hours);
                    Add(endExtra, entry.WorkId, paid);
                    break;
                case WorkChangeType.TravelWithin:
                    AddWithin(within, entry, paid);
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

    private static void AddWithin(Dictionary<Guid, List<(DateTime At, decimal Hours)>> target, Entry entry, decimal paid)
    {
        if (!target.TryGetValue(entry.WorkId, out var list))
        {
            list = [];
            target[entry.WorkId] = list;
        }
        list.Add((entry.At, paid));
    }
}
