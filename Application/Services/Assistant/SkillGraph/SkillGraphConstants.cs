// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants for the emergent skill-relationship graph. Phase 0 only carries the grounded
/// substrate-prior values; learning thresholds (acting, exploration) arrive in later phases.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.SkillGraph;

public static class SkillGraphConstants
{
    public const double SubstratePriorConfidence = 0.4;

    public const string SubstratePriorProvenance = "substrate-prior";
}
