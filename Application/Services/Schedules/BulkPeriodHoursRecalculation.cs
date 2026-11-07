// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Persists the period hours after a committed write of several entries, once per (client, scenario token, real pay
/// period), in the order the entries are given: readers prefer a cached row over the live sum, and the payroll export
/// reads only the cached rows. After the overtime cascade the following pay period is recalculated as well when an
/// entry's day plus a week passes its period end (OvertimeSuccessorReach), because the cascade changed the surcharges of
/// successors there. Rows are stored without a push. Frozen periods stay unchanged
/// (IPeriodHoursService.RecalculatePeriodHoursAsync).
/// </summary>

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public static class BulkPeriodHoursRecalculation
{
    /// <summary>Recalculates the own pay period of every entry and, when asked, the following one it reaches.</summary>
    /// <param name="periodHoursService">Resolves the real pay periods and stores the recalculated rows</param>
    /// <param name="entries">Client, day and scenario token of every written entry</param>
    /// <param name="includeSuccessorPeriod">True after an overtime cascade: also the following period when it can be reached</param>
    public static Task RecalculateAsync(
        IPeriodHoursService periodHoursService,
        IEnumerable<(Guid ClientId, DateOnly Date, Guid? AnalyseToken)> entries,
        bool includeSuccessorPeriod)
        => RunAsync(periodHoursService, entries, ownPeriods: true, includeSuccessorPeriod, knownPeriods: null);

    /// <summary>
    /// Recalculates only the following pay periods the entries reach, for callers that already stored (and pushed) the
    /// entries' own periods; a following period that is the own period of another entry of the same person and token is
    /// skipped.
    /// </summary>
    /// <param name="periodHoursService">Resolves the real pay periods and stores the recalculated rows</param>
    /// <param name="entries">Client, day and scenario token of every written entry</param>
    /// <param name="knownPeriods">Pay periods the caller already resolved per day, so they are not resolved again</param>
    public static Task RecalculateFollowingPeriodsAsync(
        IPeriodHoursService periodHoursService,
        IEnumerable<(Guid ClientId, DateOnly Date, Guid? AnalyseToken)> entries,
        IReadOnlyDictionary<DateOnly, (DateOnly Start, DateOnly End)>? knownPeriods = null)
        => RunAsync(periodHoursService, entries, ownPeriods: false, followingPeriods: true, knownPeriods);

    private static async Task RunAsync(
        IPeriodHoursService periodHoursService,
        IEnumerable<(Guid ClientId, DateOnly Date, Guid? AnalyseToken)> entries,
        bool ownPeriods,
        bool followingPeriods,
        IReadOnlyDictionary<DateOnly, (DateOnly Start, DateOnly End)>? knownPeriods)
    {
        var entryList = entries.ToList();
        var periodsByDate = knownPeriods?.ToDictionary(pair => pair.Key, pair => pair.Value)
            ?? new Dictionary<DateOnly, (DateOnly Start, DateOnly End)>();
        var recalculated = new HashSet<(Guid ClientId, Guid? AnalyseToken, DateOnly Start, DateOnly End)>();

        async Task<(DateOnly Start, DateOnly End)> PeriodOfAsync(DateOnly date)
        {
            if (!periodsByDate.TryGetValue(date, out var period))
            {
                period = await periodHoursService.GetPeriodBoundariesAsync(date);
                periodsByDate[date] = period;
            }

            return period;
        }

        async Task RecalculateOnceAsync(Guid clientId, Guid? analyseToken, (DateOnly Start, DateOnly End) period)
        {
            if (recalculated.Add((clientId, analyseToken, period.Start, period.End)))
            {
                await periodHoursService.RecalculatePeriodHoursAsync(clientId, period.Start, period.End, analyseToken);
            }
        }

        if (!ownPeriods)
        {
            foreach (var (clientId, date, analyseToken) in entryList)
            {
                var own = await PeriodOfAsync(date);
                recalculated.Add((clientId, analyseToken, own.Start, own.End));
            }
        }

        foreach (var (clientId, date, analyseToken) in entryList)
        {
            var period = await PeriodOfAsync(date);
            if (ownPeriods)
            {
                await RecalculateOnceAsync(clientId, analyseToken, period);
            }

            if (followingPeriods && OvertimeSuccessorReach.ReachesFollowingPeriod(date, period.End))
            {
                await RecalculateOnceAsync(clientId, analyseToken, await PeriodOfAsync(period.End.AddDays(1)));
            }
        }
    }
}
