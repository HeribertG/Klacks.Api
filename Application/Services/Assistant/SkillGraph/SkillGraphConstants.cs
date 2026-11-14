// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
    public const string SeededExperienceProvenancePrefix = "seed:";

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

    // Phase 4 — acting (association): graph neighbours of the retrieved skills compete for the last AssociationSlots
    // retrieval places, ranked by AssociationGraphWeight * (seed score * edge confidence) + (1 - weight) * own cosine.
    // Simulated 2026-11-14 (multi-tool goldset, dense top-20 = 54/100): 3 slots 62, 2 slots 60, 5 slots 60, 10 slots 58;
    // weight 0.2 beat 0.35 and 0.5. Both chosen on the same 25 tasks. IT harness, budget 30, behind SkillAssociationGate:
    // multi-tool completeness 62 -> 66 of 100 (2 slots 65), every single-target set unchanged; without the gate 3 slots
    // cost the plain holdout set 3 hits, because the cosines of retrieval ranks 18-40 lie within a hundredth.
    public const int AssociationSlots = 3;
    public const double AssociationGraphWeight = 0.2;
    public const string AssociationSlotsConfigKey = "Assistant:SkillGraph:AssociationSlots";
    public const double ExpansionExplorationEpsilon = 0.1;

    // Overrides ExpansionExplorationEpsilon per installation. 0 switches the random skip off, which an evaluation or
    // replay run needs: the skip is not recorded on the turn, so it only shows up there as unexplained variance.
    public const string ExpansionExplorationEpsilonConfigKey = "Assistant:SkillGraph:ExpansionExplorationEpsilon";

    // Phase 4 — acting (proactive suggestion / hero); frequency cap per chat session
    public const int MaxProactiveSuggestionsPerSession = 2;
}
