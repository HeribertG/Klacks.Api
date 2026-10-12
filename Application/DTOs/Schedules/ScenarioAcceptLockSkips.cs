// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Scenario entries an accept did not take over because they fall on a locked day (sealed day or locked entry) and
/// differ from what the real plan keeps there. Unchanged copies of locked entries are not counted.
/// </summary>
/// <param name="Works">Discarded scenario works that differ from the kept real work (or have none)</param>
/// <param name="Breaks">Discarded scenario breaks without an identical kept real break</param>
/// <param name="Notes">Discarded scenario schedule notes without an identical kept real note</param>

namespace Klacks.Api.Application.DTOs.Schedules;

public readonly record struct ScenarioAcceptLockSkips(int Works, int Breaks, int Notes)
{
    public int Total => Works + Breaks + Notes;
}
