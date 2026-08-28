// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Verdict on one cluster: which kind of gap it is and, for a phrase gap, which existing skill should
/// have answered it. An explicit user correction outranks the model's guess, so TargetSkill can be set
/// without a language model ever having been asked.
/// </summary>
/// <param name="Kind">One of SkillLearningClassifications</param>
/// <param name="TargetSkill">Skill the wish should route to, phrase gaps only</param>
/// <param name="Reason">Short justification, stored as the cluster's last error when the wish is unservable</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillLearningClassification(
    Guid ClusterId,
    string Kind,
    string? TargetSkill,
    string? Reason);
