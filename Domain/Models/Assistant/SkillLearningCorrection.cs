// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A user correction of a past turn, forwarded to the case collector so the cluster of that utterance
/// learns which skill the user actually expected - the one piece of evidence the refusal path can never
/// produce.
/// </summary>
/// <param name="Signal">wrong_skill or none_needed, see SkillLearningSignals</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillLearningCorrection(
    Guid AgentId,
    string UserMessage,
    string Signal,
    string? UserId,
    string? Locale,
    string? ChosenSkill,
    string? ExpectedSkill,
    Guid? TrajectoryId);
