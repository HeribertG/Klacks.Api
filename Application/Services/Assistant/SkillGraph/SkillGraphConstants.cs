// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants for the emergent skill-relationship graph. Phase 0 only carries the grounded
/// substrate-prior values; learning thresholds (acting, exploration) arrive in later phases.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.SkillGraph;

public static class SkillGraphConstants
{
    // Phase 0 — grounded substrate prior
    public const double SubstratePriorConfidence = 0.4;
    public const string SubstratePriorProvenance = "substrate-prior";

    // Phase 2 — experience-based learning
    public const int LearningWindowDays = 30;
    public const int MinSessionsForLearning = 5;
    public const int MinSkillOccurrenceForEval = 3;
    public const int MinCoOccurrenceSupport = 3;
    public const double MinLiftForPositive = 1.5;
    public const double MaxLiftForContradiction = 1.0;
    public const double MinSequentialProbability = 0.4;
    public const double InitialLearnedConfidence = 0.5;
    public const double ReinforcementStep = 0.05;
    public const double DecayStep = 0.15;
    public const double MaxLearnedConfidence = 0.95;
    public const double RetireConfidence = 0.05;
    public const double CoRequiredActiveThreshold = 0.7;
    public const double SequentialActiveThreshold = 0.8;
    public const string LearnedCoOccurrenceProvenance = "learned:cooccurrence";
    public const string LearnedSequentialProvenance = "learned:sequential";

    // Phase 3 — human feedback (accept / dismiss)
    public const double UserAcceptConfidenceBoost = 0.2;
    public const double UserDismissConfidencePenalty = 0.3;
}
