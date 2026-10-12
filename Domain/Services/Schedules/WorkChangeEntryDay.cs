// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The day the schedule shows a work change on, mirroring the entry_date rules of get_schedule_entries: on a shift
/// that crosses midnight (EndShift before StartShift) the after-shift parts and the post-midnight parts of a
/// replacement or travel are shown on the following day, everything else on the work's day. The replacement employee
/// sees a replacement on the same day as the original employee. After-shift corrections are placed by the work's end
/// time; the stacking offset of several after-shift entries is ignored, because it would have to push an entry past
/// the shift start to change the day.
/// </summary>
/// <param name="type">Kind of the work change</param>
/// <param name="workDate">CurrentDate of the parent work</param>
/// <param name="workEndTime">End time of the parent work</param>
/// <param name="changeStartTime">Stored start time of the work change (used by the "within" kinds)</param>
/// <param name="changeHours">Duration of the work change in hours (used by ReplacementEnd)</param>
/// <param name="shiftStart">StartShift of the work's shift</param>
/// <param name="shiftEnd">EndShift of the work's shift</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Schedules;

public static class WorkChangeEntryDay
{
    public static DateOnly Of(
        WorkChangeType type,
        DateOnly workDate,
        TimeOnly workEndTime,
        TimeOnly changeStartTime,
        decimal changeHours,
        TimeOnly shiftStart,
        TimeOnly shiftEnd)
    {
        if (shiftEnd >= shiftStart)
        {
            return workDate;
        }

        var shownNextDay = type switch
        {
            WorkChangeType.ReplacementEnd => workEndTime.AddHours(-(double)changeHours) < shiftStart,
            WorkChangeType.ReplacementWithin => changeStartTime < shiftStart,
            WorkChangeType.TravelWithin => changeStartTime < shiftEnd,
            WorkChangeType.CorrectionEnd or WorkChangeType.TravelEnd or WorkChangeType.Debriefing => workEndTime < shiftStart,
            _ => false,
        };

        return shownNextDay ? workDate.AddDays(1) : workDate;
    }
}
