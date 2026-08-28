// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A single occurrence inside a learning cluster. Carries the user id so the "at least two different
/// users asked for this" threshold can be evaluated and so UserDataEraser can erase it again; the
/// utterance itself is present only as an excerpt of at most 120 characters.
/// </summary>
/// <param name="Signal">Why the case was recorded, see SkillLearningSignals</param>
/// <param name="ChosenSkill">Skill the model actually called in that turn, null when it called nothing</param>
/// <param name="ExpectedSkill">Skill the user named in an explicit correction, null otherwise</param>
/// <param name="ToolsetJson">Names of the tools that were offered to the model, at most 30</param>
/// <param name="IsGolden">True for the case whose excerpt becomes the regression golden case</param>
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class SkillLearningCase : BaseEntity
{
    public Guid ClusterId { get; set; }

    public string? UserId { get; set; }

    public string? ConversationId { get; set; }

    public string Locale { get; set; } = string.Empty;

    public string IntentExcerpt { get; set; } = string.Empty;

    public string Signal { get; set; } = string.Empty;

    public string? ChosenSkill { get; set; }

    public string? ExpectedSkill { get; set; }

    public string ToolsetJson { get; set; } = "[]";

    public Guid? TrajectoryId { get; set; }

    public bool IsGolden { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}
