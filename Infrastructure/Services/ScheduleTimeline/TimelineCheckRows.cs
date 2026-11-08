// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Services.ScheduleTimeline;

/// <summary>
/// The schedule rows one timeline check works on.
/// </summary>
/// <param name="OwnWorks">Works the checked client owns (single check); the same list as Works for a range check</param>
/// <param name="Works">Top-level works of the check, including works the client replaces on (single check)</param>
/// <param name="WorkChanges">Live changes of those works, with their replacing client</param>
/// <param name="Breaks">Top-level breaks of the check</param>
/// <param name="OnCallBreakIds">Ids of the breaks that stand for an on-call absence</param>
public sealed record TimelineCheckRows(
    List<Work> OwnWorks,
    List<Work> Works,
    List<WorkChange> WorkChanges,
    List<Break> Breaks,
    IReadOnlySet<Guid> OnCallBreakIds)
{
    /// <summary>
    /// The rows of a range check restricted to the works and breaks dated within [<paramref name="from"/>,
    /// <paramref name="to"/>], with the changes of those works. The on-call ids stay as loaded: they are a lookup.
    /// </summary>
    public TimelineCheckRows Within(DateOnly from, DateOnly to)
    {
        var works = Works.Where(w => w.CurrentDate >= from && w.CurrentDate <= to).ToList();
        var workIds = works.Select(w => w.Id).ToHashSet();
        return new TimelineCheckRows(
            works,
            works,
            WorkChanges.Where(wc => workIds.Contains(wc.WorkId)).ToList(),
            Breaks.Where(b => b.CurrentDate >= from && b.CurrentDate <= to).ToList(),
            OnCallBreakIds);
    }
}
