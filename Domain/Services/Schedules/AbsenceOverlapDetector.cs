// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// Single rule for "a work stands on an absence": an overlapping block pair of a Work block and a Break of any type
/// except on-call. Absence types carry no attribute that tells illness from vacation, so the rule covers them all.
/// The display paths report such a pair as an uncovered absence (see <see cref="HasUncoveredRest"/>); the booking
/// checks keep treating it as a collision. Other pairs, a replacement block over an absence included, stay collisions.
/// </summary>
/// <param name="onCallBreakIds">Source ids of the break blocks whose absence type is on-call</param>
public static class AbsenceOverlapDetector
{
    /// <summary>True when the pair is a work on an absence; returns the absence and the work block.</summary>
    public static bool TryGet(
        ScheduleBlock a,
        ScheduleBlock b,
        IReadOnlySet<Guid> onCallBreakIds,
        out ScheduleBlock absence,
        out ScheduleBlock work)
    {
        if (IsAbsence(a, onCallBreakIds) && b.BlockType == ScheduleBlockType.Work)
        {
            absence = a;
            work = b;
            return true;
        }
        if (IsAbsence(b, onCallBreakIds) && a.BlockType == ScheduleBlockType.Work)
        {
            absence = b;
            work = a;
            return true;
        }
        absence = a;
        work = b;
        return false;
    }

    /// <summary>
    /// True when part of the time the work overlaps the absence has no replacement. Compared on the company-local
    /// calendar, because the replacement windows are wall-clock intervals.
    /// </summary>
    /// <param name="absence">The absence block</param>
    /// <param name="work">The work block</param>
    /// <param name="replacementWindows">Wall-clock intervals handed to substitutes of this work</param>
    public static bool HasUncoveredRest(
        ScheduleBlock absence,
        ScheduleBlock work,
        IReadOnlyList<ReplacementInterval> replacementWindows)
    {
        var coveredUntil = Max(absence.CalendarStart, work.CalendarStart);
        var overlapEnd = Min(absence.CalendarEnd, work.CalendarEnd);

        foreach (var window in replacementWindows.OrderBy(window => window.Start))
        {
            if (window.Start > coveredUntil)
            {
                break;
            }

            coveredUntil = Max(coveredUntil, window.End);
        }

        return coveredUntil < overlapEnd;
    }

    private static bool IsAbsence(ScheduleBlock block, IReadOnlySet<Guid> onCallBreakIds)
        => block.BlockType == ScheduleBlockType.Break && !OnCallOverlapDetector.IsOnCallBreak(block, onCallBreakIds);

    private static DateTime Max(DateTime first, DateTime second) => first > second ? first : second;

    private static DateTime Min(DateTime first, DateTime second) => first < second ? first : second;
}
