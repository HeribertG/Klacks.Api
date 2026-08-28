// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One cluster as the classifier sees it: the wish plus the skills the live retrieval currently offers
/// for it. The candidate list is what makes the question answerable at all - without it the model would
/// have to be told the whole catalogue to judge whether a fitting skill exists.
/// </summary>
/// <param name="CandidateSkills">Names the toolset assembler produced for the excerpt, best first</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillLearningTriageInput(
    SkillLearningClusterContext Cluster,
    IReadOnlyList<string> CandidateSkills);
