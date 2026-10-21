// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Aggregated hard-constraint inputs for a wizard run.
/// </summary>
/// <param name="ScheduleCommands">Per-day planning keywords (FREE/EARLY/LATE/NIGHT and their negations)</param>
/// <param name="ShiftPreferences">Master-data preferences per client and shift</param>
/// <param name="BreakBlockers">Absence/break blockers (one entry per affected day)</param>
/// <param name="LockedWorks">Existing Work entities with LockLevel &gt; 0 (lifted into the genome as locked tokens)</param>
/// <param name="ExistingWorkBlockers">Existing Work entities with LockLevel = None — veto source only, never lifted into the genome</param>
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Interfaces.Schedules;

public sealed record HardConstraintResult(
    IReadOnlyList<CoreScheduleCommand> ScheduleCommands,
    IReadOnlyList<CoreShiftPreference> ShiftPreferences,
    IReadOnlyList<CoreBreakBlocker> BreakBlockers,
    IReadOnlyList<CoreLockedWork> LockedWorks,
    IReadOnlyList<CoreExistingWorkBlocker> ExistingWorkBlockers);
