// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules.Summary;

/// <summary>
/// Live summary of one scenario, computed from the scenario's own shifts and works, so it stays true after manual edits.
/// </summary>
/// <param name="Token">Token of the scenario.</param>
/// <param name="FromDate">First day of the scenario period.</param>
/// <param name="UntilDate">Last day of the scenario period.</param>
/// <param name="AgentCount">Employees in the scenario's group scope that were considered.</param>
/// <param name="WorkCount">Top-level works the scenario holds in the period.</param>
/// <param name="DemandedSlots">Employee slots all planned shifts need in the period.</param>
/// <param name="FilledSlots">Slots covered by a work.</param>
/// <param name="Shifts">Coverage per shift.</param>
/// <param name="OpenSlotReasons">Open slots grouped by cause, largest first.</param>
/// <param name="ExpensesWithChangedEmployee">Expenses and reimbursements of the period that now sit on a work of another
/// employee than the work it was cloned from (a wizard or harmonizer run handed the work, or the slot, to someone else).</param>
/// <param name="Variants">Coverage per variant (key) the scenario carries, ordered by name; null when the scenario carries
/// none, or none of their shifts is planned and none holds a copy of a live real order (e.g. after the scenario was
/// accepted), so a plain scenario keeps its former shape.</param>
public sealed record ScenarioSummaryDto(
    Guid Token,
    DateOnly FromDate,
    DateOnly UntilDate,
    int AgentCount,
    int WorkCount,
    int DemandedSlots,
    int FilledSlots,
    IReadOnlyList<ScenarioShiftCoverageDto> Shifts,
    IReadOnlyList<ScenarioOpenSlotReasonDto> OpenSlotReasons,
    int ExpensesWithChangedEmployee = 0,
    IReadOnlyList<ScenarioVariantCoverageDto>? Variants = null)
{
    public int OpenSlots => DemandedSlots - FilledSlots;
}
