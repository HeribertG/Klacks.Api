// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Everything one learning round knows about a cluster after its cases were read: the wish itself and the
/// evidence around it. Deliberately a value rather than the entity plus its cases, so the generator and
/// the oracles cannot reach for a user id or a raw message the loop must never hand to a language model.
/// </summary>
/// <param name="ExpectedSkill">Skill a user named in an explicit correction, null when nobody did</param>
/// <param name="ChosenSkill">Skill the model called instead, null when it called nothing</param>
/// <param name="OfferedTools">Tool names that were offered in the turn the wish was refused in</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillLearningClusterContext(
    Guid ClusterId,
    string IntentExcerpt,
    string Locale,
    string? ExpectedSkill,
    string? ChosenSkill,
    IReadOnlyList<string> OfferedTools,
    int AttemptCount,
    string? LastError);
