// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Every number of the graceful-correction path (TP1): how long a previous action can anchor a
/// correction, how long its row survives, how close two candidates may be before the assistant asks
/// instead of guessing, and the storage caps.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class GracefulCorrectionDefaults
{
    /// <summary>
    /// How soon after the assistant's turn a negation is trusted as a reactive correction of THAT turn
    /// rather than an unrelated later message that happens to contain a negation. Single source for both
    /// consumers: gate G0 of the correction path and the implicit-correction window of the learning
    /// trajectory (TrajectoryCaptureService.ImplicitCorrectionWindow), so the learning loop and the
    /// routing loop can never disagree about which turn a user just corrected.
    /// </summary>
    public const int CorrectionWindowMinutes = 2;

    /// <summary>
    /// Row lifetime of a previous-action record. Deliberately the same five minutes as
    /// AutonomyDefaults.ConfirmationTtlMinutes: the row stores user text and tool arguments, i.e. the
    /// same data class as a pending confirmation, so it gets the same retention rather than a longer one.
    /// Longer than CorrectionWindowMinutes on purpose - the row also carries the clarification pins,
    /// which are read after the anchor itself has gone stale.
    /// </summary>
    public const int LastActionTtlMinutes = 5;

    /// <summary>
    /// Cap for the stored user message, shared with the pending recipe trigger message so a correction
    /// composite built from either source is capped identically.
    /// </summary>
    public const int UserMessageMaxLength = RecipeEngineDefaults.PendingRecipeTriggerMessageMaxLength;

    /// <summary>Cap for the stored assistant answer excerpt, the text the note quotes back.</summary>
    public const int AnswerExcerptMaxLength = 120;

    /// <summary>
    /// Authored label of the called skill, sized like the answer excerpt. Applies to the label the
    /// previous turn resolved for itself AND to every entry of the authored label dictionary the record
    /// carries, so no single language can inflate the stored row.
    /// </summary>
    public const int SkillDisplayLabelMaxLength = 120;

    /// <summary>Cap for one call's serialized arguments and for its serialized result data.</summary>
    public const int CallJsonMaxLength = 2000;

    /// <summary>
    /// Maximum score distance between the two best deterministic candidates at which the assistant asks
    /// which one was meant instead of picking. Start value, calibrated by correction-v1 (item
    /// cr-de-005-ambiguous). Task 5a's rule: both candidates scored AND within this tolerance of each
    /// other -> ask; neither candidate scored -> ask (a deliberate bias towards the question, see design
    /// rule 2); exactly one candidate scored -> act on it, the unscored one never competes.
    /// </summary>
    public const double CorrectionAmbiguityTolerance = 0.05;

    /// <summary>Number of options the clarification question may offer. Exactly two, never a menu.</summary>
    public const int ClarificationCandidateCount = 2;

    /// <summary>
    /// Maximum characters of a skill's authored label (AgentSkill.Labels, resolved by SkillLabelResolver)
    /// when it is offered as one of the two options of the clarification. Shorter than
    /// SkillDisplayLabelMaxLength on purpose: two options and the previous action share one sentence.
    /// </summary>
    public const int OptionLabelMaxLength = 80;

    /// <summary>Column length of the conversation key, shared by the row configuration and the store.</summary>
    public const int ConversationIdMaxLength = 128;

    /// <summary>Serialized empty payloads, so no "{}"/"[]" literal is repeated across model and store.</summary>
    public const string EmptyJsonObject = "{}";

    public const string EmptyJsonArray = "[]";
}
