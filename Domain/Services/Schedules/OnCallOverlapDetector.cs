// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// Single rule for "call-out during on-call duty": an overlapping block pair in which exactly one side is an
/// on-call break and the other side is not a break. Such a pair is not a collision but an on-call overlap
/// (Warning). Break-on-break and work-on-work pairs stay collisions. Shared by the validation builders and
/// the live collision list so both judge a pair identically.
/// </summary>
/// <param name="onCallBreakIds">Source ids of the break blocks whose absence type is on-call</param>
public static class OnCallOverlapDetector
{
    public static readonly IReadOnlySet<Guid> NoOnCallBreaks = new HashSet<Guid>();

    /// <summary>True when the pair is an on-call overlap; returns the on-call break and the other block.</summary>
    public static bool TryGet(
        ScheduleBlock a,
        ScheduleBlock b,
        IReadOnlySet<Guid> onCallBreakIds,
        out ScheduleBlock onCall,
        out ScheduleBlock work)
    {
        if (IsOnCallBreak(a, onCallBreakIds) && b.BlockType != ScheduleBlockType.Break)
        {
            onCall = a;
            work = b;
            return true;
        }
        if (IsOnCallBreak(b, onCallBreakIds) && a.BlockType != ScheduleBlockType.Break)
        {
            onCall = b;
            work = a;
            return true;
        }
        onCall = a;
        work = b;
        return false;
    }

    public static bool IsOnCallBreak(ScheduleBlock block, IReadOnlySet<Guid> onCallBreakIds)
        => block.BlockType == ScheduleBlockType.Break && onCallBreakIds.Contains(block.SourceId);
}
