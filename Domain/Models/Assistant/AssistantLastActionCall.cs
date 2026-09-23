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
    /// The skill's authored label (AgentSkill.Labels) as SkillLabelResolver resolved it FOR THE TURN THAT
    /// MADE THE CALL - i.e. the noun the assistant itself used in the answer the user is now correcting.
    /// Read by the model-facing correction note, which quotes what was said rather than re-translating
    /// it. Null when no label was authored for that turn's language; the caller then falls back to
    /// GracefulCorrectionNotes.UnnamedPreviousActionLabel, never to the internal snake_case name.
    /// That stand-in is English because the note it lands in is model-facing - the German user-facing
    /// redaction (MutationGuardConstants.RedactedInternalIdentifier) belongs in front of a user, not in
    /// an English instruction.
    /// NOT what the user-facing clarification names the previous action with: that one resolves from
    /// SkillLabels in the CORRECTION turn's language.
    /// </summary>
    public string? SkillDisplayLabel { get; set; }

    /// <summary>
    /// The skill's authored labels per language tag (AgentSkill.Labels -> LLMFunction.Labels), copied
    /// from the toolset of the turn that made the call. They have to be captured HERE, because by the
    /// time the correction turn runs the skill is excluded from that turn's toolset and can no longer be
    /// looked up. Carrying the whole dictionary rather than only the resolved label is what binds the
    /// clarification to the language of the CORRECTION rather than to the language of the action: a user
    /// who switches UI language inside the two-minute window is asked in the language they switched to,
    /// which is the one-language rule (spec section 1 rule 4). Null or empty when the skill carries no
    /// authored labels at all - the correction then asks no question rather than naming the
    /// misunderstanding in a foreign language.
    /// </summary>
    public IReadOnlyDictionary<string, string>? SkillLabels { get; set; }

    /// <summary>Serialized invocation arguments, capped at GracefulCorrectionDefaults.CallJsonMaxLength.</summary>
    public string ArgumentsJson { get; set; } = Constants.GracefulCorrectionDefaults.EmptyJsonObject;

    /// <summary>
    /// Serialized result data of the call (PascalCase keys, the shape LLMFunctionExecutor produces),
    /// capped the same way. Carries the created entity id a create/delete undo pair needs. Empty when
    /// the skill returned no data.
    /// </summary>
    public string ResultDataJson { get; set; } = Constants.GracefulCorrectionDefaults.EmptyJsonObject;

    /// <summary>
    /// Whether the call was read-only by the NAME PREFIX rule of ReadOnlySkillPrefixes alone, deliberately
    /// not the category-first rule of SkillRiskClassifier. RepeatedWriteCallGuard starts from the same
    /// prefix rule but additionally treats navigate_to and the catalogued read-only actions of multi-action
    /// skills (ReadOnlySkillActions, e.g. manage_pending_notes with action "read") as repeatable; this flag
    /// does not, so such a call reads as a write here. The prefix rule and the category rule of
    /// SkillRiskClassifier disagree in BOTH directions: a write-category skill whose name starts with a
    /// read-only prefix reads as read-only here and as a write in SkillRiskClassifier - that set is
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
