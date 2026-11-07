// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// How far the overtime cascade (K3/K4) of a changed Work can reach: its successors lie at most a week after the Work's
/// day, so the pay period after the Work's own one is affected only when that day plus a week passes the period end.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Schedules;

public static class OvertimeSuccessorReach
{
    public const int LookAheadDays = CalendarConstants.DaysPerWeek;

    /// <summary>True when successors of a Work on <paramref name="day"/> can lie after <paramref name="periodEnd"/>.</summary>
    /// <param name="day">Day of the changed Work</param>
    /// <param name="periodEnd">Last day of the Work's pay period</param>
    public static bool ReachesFollowingPeriod(DateOnly day, DateOnly periodEnd)
        => day.AddDays(LookAheadDays) > periodEnd;
}
