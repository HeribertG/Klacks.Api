// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Timing of the next-period scheduling readiness check. Defined here, like ProactiveHeartbeat,
/// so the planning window is a named installation constant rather than a magic number inside the
/// detector that watches it.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class NextPeriodScheduling
{
    /// <summary>
    /// Days of planning work that sit ABOVE the configured planning deadline: the detector reacts this many
    /// days before the latest allowed finish date, so a draft can still be prepared and reviewed in time.
    /// With no PLANNING_DEADLINE_LEAD_DAYS configured the deadline lead is 0 and this alone decides, which
    /// keeps the behavior every installation had before the setting existed.
    /// </summary>
    public const int PlanningWindowDays = 7;

    /// <summary>Margin the auto-commit watcher waits beyond the AutoWizard's own total budget.</summary>
    public const int AutoCommitWatchMarginMinutes = 5;

    /// <summary>
    /// Minutes the auto-commit watcher waits for the wizard chain before it gives up: the whole AutoWizard budget
    /// (strict chain plus soft-keyword retry) and a margin, so a finished strict plan is never reported as timed out.
    /// </summary>
    public const int AutoCommitWatchTimeoutMinutes = AutoWizardTimeBudget.TotalMinutes + AutoCommitWatchMarginMinutes;

    /// <summary>How much longer than the watch window the interrupted check waits.</summary>
    public const int AutoCommitInterruptedGraceMarginMinutes = 10;

    /// <summary>
    /// How old an automatic autofill run has to be before a still-unaccepted draft with no running
    /// watcher counts as interrupted. Longer than AutoCommitWatchTimeoutMinutes on purpose: inside that
    /// window a watcher on another API instance may legitimately still be working on the same chain,
    /// and this instance cannot see its in-memory job registry.
    /// </summary>
    public const int AutoCommitInterruptedGraceMinutes = AutoCommitWatchTimeoutMinutes + AutoCommitInterruptedGraceMarginMinutes;
}
