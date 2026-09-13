// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of a targeted holdout replay. Measured false means the gate could not run at all - no full
/// eval run, no goldset, no holdout item that involves the skill, or not a single replay the provider
/// answered - and is NOT a pass: a change nothing measured must stay pending rather than go live on an
/// empty verdict.
/// </summary>
/// <param name="Measured">Whether any holdout item was actually replayed and answered</param>
/// <param name="Regressions">Human-readable description of every previously passing item that now fails</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record GoldsetHoldoutReplayVerdict(bool Measured, IReadOnlyList<string> Regressions)
{
    public static GoldsetHoldoutReplayVerdict NotMeasured { get; } = new(false, []);
}
