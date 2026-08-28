// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A negation in the turn that followed, read as a correction of the preceding utterance. Carries the
/// cluster key and the excerpt of that preceding turn rather than its text: the raw message is gone by
/// then, and only the stored hash points at the same cluster the refusal and correction paths use.
/// </summary>
/// <param name="ClusterKey">MessageNormalizer hash of the preceding utterance, taken from its trajectory</param>
/// <param name="IntentExcerpt">Stored excerpt of the preceding utterance, at most 120 characters</param>
/// <param name="ToolsetJson">Names of the tools that were offered for the preceding turn</param>
/// <param name="TrajectoryId">Trajectory of the preceding turn, the one just flagged as corrected</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillLearningImplicitCorrection(
    Guid AgentId,
    string ClusterKey,
    string IntentExcerpt,
    string? UserId,
    string? Locale,
    string? ChosenSkill,
    string ToolsetJson,
    Guid TrajectoryId);
