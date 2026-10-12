// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Scenario entries an accept did not take over because they fall on a locked day (sealed day, locked entry, or a
/// replacement with an employee whose day is locked) and differ from what the real plan keeps there. Unchanged copies
/// of locked entries are not counted. Each count is per entry, not per work: a discarded work with an edited expense
/// counts once under Expenses and, only when the work itself differs too, once under Works.
/// </summary>
/// <param name="Works">Discarded scenario works that differ from the kept real work (or have none)</param>
/// <param name="WorkChanges">Work changes of discarded scenario works without an identical one on the kept real work, or removed from it</param>
/// <param name="Expenses">Expenses of discarded scenario works without an identical one on the kept real work, or removed from it</param>
/// <param name="Breaks">Discarded scenario breaks without an identical kept real break</param>
/// <param name="Notes">Discarded scenario schedule notes without an identical kept real note</param>

namespace Klacks.Api.Application.DTOs.Schedules;

public readonly record struct ScenarioAcceptLockSkips(int Works, int Breaks, int Notes, int WorkChanges, int Expenses)
{
    public int Total => Works + Breaks + Notes + WorkChanges + Expenses;
}
