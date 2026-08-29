// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// An activated learning artefact seen from the outside: what it is, what it is called, and which
/// cluster and candidate it came from. Resolved from the cluster rather than from the candidate payload,
/// because the cluster is where the outcome reference lives and is the only place both artefact kinds
/// are described the same way.
/// </summary>
/// <param name="Kind">phrase or capability, see SkillLearningOutcomeKinds</param>
/// <param name="OwnerName">Skill a learned phrase belongs to, or the name of a learned recipe</param>
/// <param name="PhraseId">Id of the skill_phrase row, null for a capability</param>
/// <param name="CandidateId">Candidate the fitness rows hang off, null when none is active any more</param>
/// <param name="ActivatedAtUtc">When the artefact went live; the clock an unused artefact ages on</param>
/// <param name="ExecutionUnproven">True when the execution oracle could not run every step, so the artefact still owes a first real run</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record LearnedArtefact(
    Guid ClusterId,
    string Kind,
    string OwnerName,
    Guid? PhraseId,
    Guid? CandidateId,
    DateTime ActivatedAtUtc,
    bool ExecutionUnproven);
