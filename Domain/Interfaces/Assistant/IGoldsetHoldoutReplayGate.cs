// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Replays the holdout goldset items that involve one skill against the live catalogue, so a description
/// narrowed from goldset evidence is judged on data it was never trained on.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IGoldsetHoldoutReplayGate
{
    /// <summary>
    /// Judges the CURRENTLY LIVE description of the named skill. The caller must have applied the change
    /// and refreshed the catalogue before calling, and must put the old description back afterwards when
    /// the verdict is negative.
    /// </summary>
    Task<GoldsetHoldoutReplayVerdict> EvaluateAsync(
        string sharpenedSkillName, CancellationToken cancellationToken = default);
}
