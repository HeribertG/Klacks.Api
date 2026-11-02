// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules.Recovery;

/// <summary>
/// The time a work carries around its interval, summed from its non-replacement work changes: the lead before the
/// start (briefing, start correction, start travel), the trail after the end (debriefing, end correction, end travel)
/// and the paid hours of travel and corrections. A briefing or debriefing adds occupied time but no paid hours.
/// </summary>
/// <param name="LeadHours">Hours directly before the start</param>
/// <param name="TrailHours">Hours directly after the end</param>
/// <param name="StartExtraPaidHours">Paid hours of the start-side corrections and travel</param>
/// <param name="EndExtraPaidHours">Paid hours of the end-side corrections and travel</param>
/// <param name="WithinExtraPaidHours">Paid hours of travel inside the work, with the instant the travel starts</param>
public sealed record RecoveryWorkPadding(
    decimal LeadHours,
    decimal TrailHours,
    decimal StartExtraPaidHours,
    decimal EndExtraPaidHours,
    IReadOnlyList<(DateTime At, decimal Hours)> WithinExtraPaidHours)
{
    public static readonly RecoveryWorkPadding None = new(0m, 0m, 0m, 0m, []);

    /// <summary>
    /// The padding one segment of the work inherits. A segment keeps the lead only when it begins at the start of the
    /// work and the trail only when it ends at the end of the work; start-side and end-side paid hours follow the
    /// same rule, and travel inside the work goes with the segment it starts in (the first segment when it starts
    /// inside a part that was handed away).
    /// </summary>
    /// <param name="workStart">Start of the whole work</param>
    /// <param name="workEnd">End of the whole work</param>
    /// <param name="segments">The parts of the work the original still holds, in time order</param>
    /// <param name="index">The segment to answer for</param>
    public (decimal LeadHours, decimal TrailHours, decimal ExtraPaidHours) ForSegment(
        DateTime workStart,
        DateTime workEnd,
        IReadOnlyList<(DateTime Start, DateTime End)> segments,
        int index)
    {
        var segment = segments[index];
        var atStart = segment.Start == workStart;
        var atEnd = segment.End == workEnd;
        var extra = (atStart ? StartExtraPaidHours : 0m) + (atEnd ? EndExtraPaidHours : 0m);
        foreach (var (at, hours) in WithinExtraPaidHours)
        {
            var owner = FindOwner(segments, at);
            if (owner == index)
            {
                extra += hours;
            }
        }

        return (atStart ? LeadHours : 0m, atEnd ? TrailHours : 0m, extra);
    }

    private static int FindOwner(IReadOnlyList<(DateTime Start, DateTime End)> segments, DateTime at)
    {
        for (var i = 0; i < segments.Count; i++)
        {
            if (at >= segments[i].Start && at < segments[i].End)
            {
                return i;
            }
        }
        return 0;
    }
}
