// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Timing of the next-period scheduling readiness check. Defined here, like ProactiveHeartbeat,
/// so the lead window is a named installation constant rather than a magic number inside the
/// detector that watches it.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class NextPeriodScheduling
{
    /// <summary>Days before the next pay-period's start by which a schedule draft should exist.</summary>
    public const int LeadTimeDays = 7;

    /// <summary>Minutes the auto-commit watcher waits for the wizard chain before it gives up.</summary>
    public const int AutoCommitWatchTimeoutMinutes = 20;

    /// <summary>
    /// How old an automatic autofill run has to be before a still-unaccepted draft with no running
    /// watcher counts as interrupted. Longer than AutoCommitWatchTimeoutMinutes on purpose: inside that
    /// window a watcher on another API instance may legitimately still be working on the same chain,
    /// and this instance cannot see its in-memory job registry.
    /// </summary>
    public const int AutoCommitInterruptedGraceMinutes = 30;
}
