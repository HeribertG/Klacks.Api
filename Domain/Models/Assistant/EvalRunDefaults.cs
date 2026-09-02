// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence defaults for <see cref="EvalRun"/> that both the EF configuration and the
/// migration must agree on.
/// </summary>

namespace Klacks.Api.Domain.Models.Assistant;

public static class EvalRunDefaults
{
    /// <summary>
    /// Scorer version assumed for rows written before scoring was versioned. Every historical run
    /// is version 1 and therefore never a baseline for a version 2 run.
    /// </summary>
    public const int LegacyScorerVersion = 1;
}
