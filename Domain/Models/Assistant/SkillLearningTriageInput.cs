// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One cluster as the classifier sees it: the wish plus two lists that must never be confused, because
/// they answer different questions and used to be the same list - which made phrase learning impossible.
/// CandidateSkills answers "is this wish already served" and is therefore the dismissal criterion.
/// ReachableSkills answers "which skills may be named at all" and is therefore the selection menu. When
/// the selection menu equals the dismissal criterion, every nameable target is by construction already
/// routed and no classifier-chosen wish can ever be learned.
/// </summary>
/// <param name="CandidateSkills">Names the toolset assembler produced for the excerpt, best first</param>
/// <param name="ReachableSkills">
/// Names the retrieval stage can reach for the excerpt, a superset of CandidateSkills' retrieved half.
/// Bounded by the reranker pool, so every entry is a real, permitted, indexed skill.
/// </param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillLearningTriageInput(
    SkillLearningClusterContext Cluster,
    IReadOnlyList<string> CandidateSkills,
    IReadOnlyList<string> ReachableSkills);
