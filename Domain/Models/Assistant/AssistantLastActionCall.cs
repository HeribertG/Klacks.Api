// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One tool call of the previous assistant turn, as much of it as the correction path needs: the skill
/// name to exclude from the next toolset, the arguments and the result data an undo would replay, and
/// whether it was a write at all.
/// </summary>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed class AssistantLastActionCall
{
    public string SkillName { get; set; } = string.Empty;

    /// <summary>
    /// User-facing label of the skill, captured from the toolset of the turn that made the call. The
    /// correction note and the clarification name this instead of the internal snake_case name - and it
    /// has to be captured HERE, because by the time the correction turn runs the skill is excluded from
    /// the toolset and can no longer be looked up. Null when the turn had no description for it; the
    /// caller then falls back to MutationGuardConstants.RedactedInternalIdentifier, never to the name.
    /// </summary>
    public string? SkillDisplayLabel { get; set; }

    /// <summary>Serialized invocation arguments, capped at GracefulCorrectionDefaults.CallJsonMaxLength.</summary>
    public string ArgumentsJson { get; set; } = Constants.GracefulCorrectionDefaults.EmptyJsonObject;

    /// <summary>
    /// Serialized result data of the call (PascalCase keys, the shape LLMFunctionExecutor produces),
    /// capped the same way. Carries the created entity id a create/delete undo pair needs. Empty when
    /// the skill returned no data.
    /// </summary>
    public string ResultDataJson { get; set; } = Constants.GracefulCorrectionDefaults.EmptyJsonObject;

    /// <summary>
    /// Whether the call was read-only by the NAME PREFIX rule of ReadOnlySkillPrefixes - the same rule
    /// the multi-turn loop applies in RejectRepeatedWriteCalls, deliberately not the category-first rule
    /// of SkillRiskClassifier. The two rules disagree in BOTH directions: a write-category skill whose
    /// name starts with a read-only prefix reads as read-only here and as a write there - that set is
    /// pinned by ReadOnlyPrefixWriteCategoryGuardTests, so it can shrink but not silently grow - and a
    /// read-category skill whose name carries no read-only prefix (82 skills, e.g. navigate_to,
    /// web_search, select_group, every explain_*) reads as a write here and as read-only there - that
    /// set is large and not pinned. Because of the second direction, the undo path must never gate on
    /// IsReadOnly alone; it must also require an available inverse and Success.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Whether the call itself succeeded. A FAILED call is recorded too: the user corrects what the
    /// assistant did, and a failed attempt is just as much a wrong interpretation as a successful one.
    /// This flag is what keeps that safe downstream - the undo path offers a rollback only for a write
    /// that actually landed (spec section 4.6), so a failed call anchors a correction without ever
    /// producing an undo offer for something that never happened.
    /// </summary>
    public bool Success { get; set; }
}
