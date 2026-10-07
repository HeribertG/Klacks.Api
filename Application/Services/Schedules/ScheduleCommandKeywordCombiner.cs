// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Reduces several schedule commands of one (agent, day) to the single keyword that expresses their combined
/// restriction, the way Wizard 1 already reads them (every command applies). It works on the set of allowed shift
/// kinds {Early, Late, Night}: OnlyX keeps only X, NoX removes X, FREE removes all, -FREE (NotFree) removes none.
/// Three kinds left = NotFree, two = No&lt;missing kind&gt;, one = Only&lt;kind&gt;, none = Free. Wizard 2 and the
/// recovery snapshot hold one keyword per day, so they use this instead of "last wins" or "lowest enum value".
/// </summary>
public static class ScheduleCommandKeywordCombiner
{
    private const int Early = 1;
    private const int Late = 2;
    private const int Night = 4;
    private const int AllKinds = Early | Late | Night;
    private const int NoKind = 0;

    /// <summary>
    /// Combined keyword of the given commands, or null when there are none.
    /// </summary>
    /// <param name="keywords">The recognized keywords of one (agent, day), in any order.</param>
    public static ScheduleCommandKeyword? Combine(IEnumerable<ScheduleCommandKeyword> keywords)
    {
        var any = false;
        var allowed = AllKinds;
        foreach (var keyword in keywords)
        {
            any = true;
            allowed = Restrict(allowed, keyword);
        }

        return any ? ToKeyword(allowed) : null;
    }

    private static int Restrict(int allowed, ScheduleCommandKeyword keyword) => keyword switch
    {
        ScheduleCommandKeyword.Free => NoKind,
        ScheduleCommandKeyword.OnlyEarly => allowed & Early,
        ScheduleCommandKeyword.OnlyLate => allowed & Late,
        ScheduleCommandKeyword.OnlyNight => allowed & Night,
        ScheduleCommandKeyword.NoEarly => allowed & ~Early,
        ScheduleCommandKeyword.NoLate => allowed & ~Late,
        ScheduleCommandKeyword.NoNight => allowed & ~Night,
        _ => allowed,
    };

    private static ScheduleCommandKeyword ToKeyword(int allowed) => allowed switch
    {
        NoKind => ScheduleCommandKeyword.Free,
        Early => ScheduleCommandKeyword.OnlyEarly,
        Late => ScheduleCommandKeyword.OnlyLate,
        Night => ScheduleCommandKeyword.OnlyNight,
        Late | Night => ScheduleCommandKeyword.NoEarly,
        Early | Night => ScheduleCommandKeyword.NoLate,
        Early | Late => ScheduleCommandKeyword.NoNight,
        _ => ScheduleCommandKeyword.NotFree,
    };
}
