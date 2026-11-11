// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.ScheduleRecovery.Model;

namespace Klacks.Api.Application.Services.Schedules.Recovery;

/// <summary>
/// The days around an absence on which a swap chain of the recovery engine may take a work from its holder.
/// </summary>
internal static class RecoveryReliefDays
{
    /// <summary>
    /// The days within the relief reach of the engine around the absence days, without the absence days themselves
    /// and never outside the window the snapshot reads, ascending.
    /// </summary>
    /// <param name="absenceDates">Days of the absence</param>
    /// <param name="windowStart">First day the snapshot reads</param>
    /// <param name="windowEnd">Last day the snapshot reads</param>
    public static IReadOnlyList<DateOnly> Around(
        IReadOnlyList<DateOnly> absenceDates, DateOnly windowStart, DateOnly windowEnd)
    {
        var absence = absenceDates.ToHashSet();
        var days = new SortedSet<DateOnly>();
        foreach (var date in absenceDates)
        {
            for (var offset = -RulesetDefaults.ReliefReachDays; offset <= RulesetDefaults.ReliefReachDays; offset++)
            {
                var day = date.AddDays(offset);
                if (day >= windowStart && day <= windowEnd && !absence.Contains(day))
                {
                    days.Add(day);
                }
            }
        }
        return [.. days];
    }
}
