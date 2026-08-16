// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure time-budget arithmetic for one escalation-chain handoff (docs/ENTWURF-eskalationskette-2026-08-16.md
/// §5/§6, decisions B3/B4). Takes no dependency on the clock, the database or delivery: given "now",
/// the frozen deadline and how many roster ranks are still Pending, it decides whether this round
/// notifies one person (serial) or everybody left (parallel), and for how long. The same call drives
/// both the initial wave at chain start and every later handoff after a stage expires, so a stage
/// left over from a rolled-back parallel wave is treated identically to one that has waited its turn.
/// </summary>

namespace Klacks.Api.Domain.Services.Assistant;

public static class EscalationWaveCalculator
{
    /// <summary>
    /// Decides the next wave: serial (only the lowest-rank pending stage) once T/N is comfortably
    /// above the floor, parallel (every pending stage at once) once the remaining budget can no
    /// longer afford to wait one out - either because the deadline has already passed (B4) or
    /// because the per-stage share would fall under the floor before it is even clamped.
    /// </summary>
    /// <param name="nowUtc">Current instant.</param>
    /// <param name="deadlineUtc">The chain's frozen deadline (EscalationChain.DeadlineUtc).</param>
    /// <param name="pendingStageCount">How many roster ranks have not been tried yet, including whichever would go next.</param>
    /// <param name="minStageMinutes">Floor for one stage's turn (ESCALATION_STAGE_MIN_MINUTES).</param>
    /// <param name="maxStageMinutes">Ceiling for one stage's turn (ESCALATION_STAGE_MAX_MINUTES).</param>
    public static EscalationWaveDecision ComputeNextWave(
        DateTime nowUtc,
        DateTime deadlineUtc,
        int pendingStageCount,
        int minStageMinutes,
        int maxStageMinutes)
    {
        if (pendingStageCount <= 0)
        {
            return new EscalationWaveDecision(IsParallel: false, StageCount: 0, Duration: TimeSpan.Zero);
        }

        var remaining = deadlineUtc - nowUtc;
        var minDuration = TimeSpan.FromMinutes(minStageMinutes);

        if (remaining <= TimeSpan.Zero)
        {
            return new EscalationWaveDecision(IsParallel: true, StageCount: pendingStageCount, Duration: minDuration);
        }

        var perStageMinutes = remaining.TotalMinutes / pendingStageCount;
        if (perStageMinutes < minStageMinutes)
        {
            return new EscalationWaveDecision(IsParallel: true, StageCount: pendingStageCount, Duration: minDuration);
        }

        var clampedMinutes = Math.Min(perStageMinutes, maxStageMinutes);
        return new EscalationWaveDecision(IsParallel: false, StageCount: 1, Duration: TimeSpan.FromMinutes(clampedMinutes));
    }
}
