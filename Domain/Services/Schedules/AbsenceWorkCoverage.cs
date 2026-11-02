// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Whether an absence takes a work away entirely. A full-day absence covers every work of its day, a work over midnight
/// included. An absence with a duration and no clock times (00:00/00:00) counts as the whole day (owner decision
/// 2026-11-02), like the absence window of the recovery engine. A partial absence covers a work only when the work lies
/// inside it; a partial overlap leaves the work to the planner, the error list shows it.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Schedules;

public static class AbsenceWorkCoverage
{
    /// <param name="absenceStart">Start of the absence on the work's day</param>
    /// <param name="absenceEnd">End of the absence</param>
    /// <param name="workStart">Start of the work</param>
    /// <param name="workEnd">End of the work; at or before the start it runs over midnight</param>
    public static bool CoversWork(TimeOnly absenceStart, TimeOnly absenceEnd, TimeOnly workStart, TimeOnly workEnd)
    {
        if (absenceStart == DayTimeConstants.Midnight
            && (absenceEnd >= DayTimeConstants.LastMinuteOfDay || absenceEnd == DayTimeConstants.Midnight))
        {
            return true;
        }

        return workEnd > workStart && absenceStart <= workStart && workEnd <= absenceEnd;
    }
}
