// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// Single rule for "a work stands on an absence": an overlapping block pair of a Work block, or a correction block
/// (correction, briefing, travel) that belongs to a work, and a Break of any type except on-call. Absence types carry
/// no attribute that tells illness from vacation, so the rule covers them all.
/// The display paths report such a pair as an uncovered absence of the work (see <see cref="HasUncoveredRest"/> and
/// <see cref="IsFullyHandedOver"/>) and no longer as a collision; the booking checks keep treating it as a collision.
/// Other pairs, a replacement block over an absence included, stay collisions.
/// </summary>
/// <param name="onCallBreakIds">Source ids of the break blocks whose absence type is on-call</param>
public static class AbsenceOverlapDetector
{
    /// <summary>
    /// True when the pair is a work on an absence; returns the absence and the work block. The work side is a Work
    /// block or a Correction block carrying its parent work (<see cref="ScheduleBlock.ParentWorkId"/>).
    /// </summary>
    public static bool TryGet(
        ScheduleBlock a,
        ScheduleBlock b,
        IReadOnlySet<Guid> onCallBreakIds,
        out ScheduleBlock absence,
        out ScheduleBlock work)
    {
        if (IsAbsence(a, onCallBreakIds) && IsWorkSide(b))
        {
            absence = a;
            work = b;
            return true;
        }
        if (IsAbsence(b, onCallBreakIds) && IsWorkSide(a))
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
        => LeavesUncovered(
            Max(absence.CalendarStart, work.CalendarStart),
            Min(absence.CalendarEnd, work.CalendarEnd),
            replacementWindows);

    /// <summary>
    /// True when substitutes together hold the whole service time of the work. The windows cover the service time only,
    /// so the correction, briefing and travel blocks around it never count as handed over on their own: they follow
    /// the verdict of their work.
    /// </summary>
    /// <param name="serviceStart">Company-local start of the work as entered</param>
    /// <param name="serviceEnd">Company-local end of the work as entered</param>
    /// <param name="replacementWindows">Wall-clock intervals handed to substitutes of this work</param>
    public static bool IsFullyHandedOver(
        DateTime serviceStart,
        DateTime serviceEnd,
        IReadOnlyList<ReplacementInterval> replacementWindows)
        => !LeavesUncovered(serviceStart, serviceEnd, replacementWindows);

    private static bool LeavesUncovered(
        DateTime from,
        DateTime to,
        IReadOnlyList<ReplacementInterval> replacementWindows)
    {
        var coveredUntil = from;

        foreach (var window in replacementWindows.OrderBy(window => window.Start))
        {
            if (window.Start > coveredUntil)
            {
                break;
            }

            coveredUntil = Max(coveredUntil, window.End);
        }

        return coveredUntil < to;
    }

    private static bool IsWorkSide(ScheduleBlock block)
        => block.BlockType == ScheduleBlockType.Work
            || (block.BlockType == ScheduleBlockType.Correction && block.ParentWorkId.HasValue);

    private static bool IsAbsence(ScheduleBlock block, IReadOnlySet<Guid> onCallBreakIds)
        => block.BlockType == ScheduleBlockType.Break && !OnCallOverlapDetector.IsOnCallBreak(block, onCallBreakIds);

    private static DateTime Max(DateTime first, DateTime second) => first > second ? first : second;

    private static DateTime Min(DateTime first, DateTime second) => first < second ? first : second;
}
