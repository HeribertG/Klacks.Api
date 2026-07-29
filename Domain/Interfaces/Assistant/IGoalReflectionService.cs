// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Runs one self-directed goal reflection cycle: collects signals, asks a cheap LLM to draft goal
/// candidates per user, and persists them in shadow mode (Phase 1 — no delivery, no plan, no skill
/// execution). See docs/superpowers/specs/2026-07-28-klacksy-selbstgesteuerte-ziele-design.md.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IGoalReflectionService
{
    /// <summary>
    /// Runs a single reflection cycle end to end.
    /// </summary>
    /// <returns>Number of new GoalCandidate rows persisted in this cycle.</returns>
    Task<int> RunReflectionCycleAsync(CancellationToken cancellationToken = default);
}
