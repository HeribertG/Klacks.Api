// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A user marking a past turn as not helpful (thumbs-down, W1.8). Forwarded to the case collector so
/// the cluster of that utterance gets an explicit negative signal - the complement of the refusal path,
/// which only ever hears about turns that produced no tool call.
/// </summary>
/// <param name="Signal">Always SkillLearningSignals.Explicit</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillLearningFeedback(
    Guid AgentId,
    string UserMessage,
    string? UserId,
    string? Locale,
    string? ChosenSkill,
    string? ToolsetJson,
    Guid? TrajectoryId);
