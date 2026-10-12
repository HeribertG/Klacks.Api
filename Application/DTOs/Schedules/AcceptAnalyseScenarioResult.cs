// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of accepting an AnalyseScenario: whether it was accepted and how many of its entries were not taken over
/// because they fall on a locked day. The counts default to 0.
/// </summary>
/// <param name="Accepted">True when the scenario was promoted into the real plan</param>
/// <param name="SkippedLockedWorks">Scenario works not taken over because of a locked day</param>
/// <param name="SkippedLockedBreaks">Scenario breaks not taken over because of a locked day</param>
/// <param name="SkippedLockedNotes">Scenario schedule notes not taken over because of a locked day</param>

namespace Klacks.Api.Application.DTOs.Schedules;

public record AcceptAnalyseScenarioResult(
    bool Accepted,
    int SkippedLockedWorks = 0,
    int SkippedLockedBreaks = 0,
    int SkippedLockedNotes = 0)
{
    public int SkippedLockedTotal => SkippedLockedWorks + SkippedLockedBreaks + SkippedLockedNotes;

    public static AcceptAnalyseScenarioResult From(ScenarioAcceptLockSkips skips)
        => new(true, skips.Works, skips.Breaks, skips.Notes);
}
