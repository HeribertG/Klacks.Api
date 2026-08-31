// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Why a single case was recorded: the assistant refused outright, the user corrected the chosen skill,
/// the user stated no skill was needed, or a negation in the following turn implied a correction.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class SkillLearningSignals
{
    public const string Refusal = "refusal";
    public const string WrongSkill = "wrong_skill";
    public const string NoneNeeded = "none_needed";
    public const string Implicit = "implicit";

    /// <summary>
    /// The user explicitly marked the turn as not helpful (thumbs-down, W1.8). The negative counterpart
    /// of the correction path: the turn was found, the user judged it, and the learning loop may cluster
    /// the utterance as one people do not get value from.
    /// </summary>
    public const string Explicit = "explicit";

    public static readonly IReadOnlyList<string> All = [Refusal, WrongSkill, NoneNeeded, Implicit, Explicit];
}
