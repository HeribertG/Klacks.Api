// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Turns a decided correction plan and the toolset that was re-assembled from it into what the chat loop
/// carries: the volatile model-facing note (always) and, when the re-routing produced no clear winner,
/// the two-option question of design rule 2. Pure and static - it reads no store, makes no model call
/// and depends on nothing but its arguments and the text catalogues, which is why it sits beside
/// TurnPreparationService rather than inside it: the service owns per-turn orchestration and its
/// dependencies, this owns one decision and its wording.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class CorrectionOutcomeComposer
{
    /// <summary>
    /// Composes the outcome of a correction turn.
    ///
    /// The undo is resolved by the caller and handed in already built, because resolving it needs a
    /// registry that lives in the Application layer while this class depends on nothing but its
    /// arguments. What is decided HERE is whether the offer is made at all: never alongside a
    /// clarification, and never when the corrected intent guaranteed no candidate - the note then already
    /// tells the model to ask for the missing detail. Either way the turn is already asking one question,
    /// and rule 3 allows exactly one yes/no offer; two questions in one answer is the dialogue the rule
    /// forbids, the affirmation that follows is ambiguous, and the token it would redeem carries a
    /// gate-bypassing write. Dropping it here rather than in the caller is deliberate: the entry points
    /// write the confirmation token from Undo, so an offer that is not made must not leave a redeemable
    /// token behind.
    /// </summary>
    /// <param name="plan">The correction the planning decided on, with the previous action it anchors to</param>
    /// <param name="assembledFunctions">The toolset re-assembled from the corrected intent</param>
    /// <param name="language">Active language of the turn</param>
    /// <param name="undo">The inverse call resolved for this correction, null when there is none</param>
    /// <param name="undoneCall">The call that inverse would undo, null when there is none</param>
    public static GracefulCorrectionOutcome Compose(
        GracefulCorrectionPlan plan,
        IReadOnlyList<LLMFunction> assembledFunctions,
        string? language,
        SkillUndoInvocation? undo,
        AssistantLastActionCall? undoneCall)
    {
        var correctedCall = plan.LastAction.Calls.FirstOrDefault();
        var previousArguments = correctedCall?.ArgumentsJson ?? GracefulCorrectionDefaults.EmptyJsonObject;

        var candidates = DeterministicCandidates(assembledFunctions)
            .OrderByDescending(f => f.RetrievalScore.HasValue)
            .ThenByDescending(f => f.RetrievalScore ?? 0.0)
            .ToList();

        var clarification = BuildClarification(candidates, QuestionLabelOf(correctedCall, language), language);

        var noteLabel = LabelOf(correctedCall) ?? GracefulCorrectionNotes.UnnamedPreviousActionLabel;

        var openingSentence = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            GracefulCorrectionNotes.OpeningSentenceTemplate,
            noteLabel);

        var note = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            GracefulCorrectionNotes.CorrectionContextTemplate,
            noteLabel,
            previousArguments,
            CapCorrection(plan.CorrectionMessage),
            openingSentence,
            AnswerLanguage(language));

        if (candidates.Count == 0)
        {
            note += GracefulCorrectionNotes.NoCandidateSuffix;
        }

        if (clarification != null)
        {
            return new GracefulCorrectionOutcome(
                note,
                clarification,
                candidates
                    .Take(GracefulCorrectionDefaults.ClarificationCandidateCount)
                    .Select(candidate => candidate.Name)
                    .ToList());
        }

        if (undo == null || candidates.Count == 0)
        {
            return new GracefulCorrectionOutcome(note, null, []);
        }

        var undoneLabel = LabelOf(undoneCall);

        note += string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            GracefulCorrectionNotes.UndoOfferTemplate,
            undoneLabel ?? GracefulCorrectionNotes.UnnamedPreviousActionLabel,
            AnswerLanguage(language));

        return new GracefulCorrectionOutcome(note, null, [], undo, undoneLabel);
    }

    /// <summary>
    /// A label for what a recorded call did AS THE TURN THAT MADE IT PUT IT, or null when that turn
    /// resolved none. Never the internal snake_case name. This is the note's label: the note is
    /// model-facing and quotes what the assistant actually told the user, so re-translating it would make
    /// the note disagree with the answer the user is correcting. Null rather than the stand-in, so the
    /// caller decides what a missing label means - the note substitutes an English model-facing stand-in.
    /// </summary>
    /// <param name="call">The recorded call of the previous turn, or null when it made none</param>
    internal static string? LabelOf(AssistantLastActionCall? call) =>
        string.IsNullOrWhiteSpace(call?.SkillDisplayLabel) ? null : call!.SkillDisplayLabel!;

    /// <summary>
    /// The noun the QUESTION names the misunderstanding with, resolved from the authored labels the
    /// record carries (AssistantLastActionCall.SkillLabels) in THIS turn's language - not the label the
    /// previous turn resolved for itself. The two differ exactly when the user switched UI language
    /// inside the two-minute correction window, and taking the stored one would then put, say, a German
    /// noun into a French sentence: the same substance violation of the one-language rule (spec section 1
    /// rule 4) that the authored labels replaced the English descriptions for.
    /// The labels are read from the record rather than looked up live because the corrected skill is
    /// excluded from this turn's toolset by construction, and TurnPreparationService (Domain) has no
    /// catalogue dependency that could answer for it.
    /// Null when the correction's language has no authored label - the question is then not asked at all,
    /// which costs one round trip and never a wrong action.
    /// </summary>
    /// <param name="call">The recorded call of the previous turn, or null when it made none</param>
    /// <param name="language">Active language of the CORRECTION turn, the one the user is asked in</param>
    internal static string? QuestionLabelOf(AssistantLastActionCall? call, string? language) =>
        SkillLabelResolver.Resolve(
            call?.SkillLabels, language, GracefulCorrectionDefaults.SkillDisplayLabelMaxLength);

    /// <summary>
    /// The correction as the note quotes it. The message is LIVE user input and, unlike the anchor's own
    /// fields, was never capped by the store, so an over-long paste would otherwise push the note past
    /// the history budget it is itself measured against. Capped to the same length the anchor's user
    /// message is stored at, by a hard slice for the reason RecipeCorrectionComposer.CapForStorage gives.
    /// </summary>
    /// <param name="correction">The user's correction message, uncapped</param>
    internal static string CapCorrection(string correction) =>
        correction.Length <= GracefulCorrectionDefaults.UserMessageMaxLength
            ? correction
            : correction[..GracefulCorrectionDefaults.UserMessageMaxLength];

    /// <summary>
    /// The deterministically guaranteed skills of the composite: keyword/synonym matches and recipe step
    /// skills. Retrieved and expanded skills are excluded on purpose - they are a ranking, and a ranking
    /// is exactly what a correction cannot be trusted to have got right. Always-on skills carry
    /// ToolsetSkillSource.AlwaysOn and are outside this set already; confirm_pending_action is removed by
    /// name because it is always-on for a different reason and is never an intent.
    /// </summary>
    /// <param name="functions">The toolset re-assembled from the corrected intent</param>
    internal static List<LLMFunction> DeterministicCandidates(IReadOnlyList<LLMFunction> functions) =>
        functions
            .Where(f => f.ToolsetSource is ToolsetSkillSource.Keyword or ToolsetSkillSource.RecipeStep)
            .Where(f => !string.Equals(
                f.Name, AutonomyDefaults.ConfirmPendingActionSkillName, StringComparison.OrdinalIgnoreCase))
            .ToList();

    /// <summary>
    /// The two-option question of design rule 2, or null when the turn proceeds without one. "Proceeding"
    /// pins and narrows nothing: the turn simply runs as an ordinary turn whose note explains the
    /// correction, and the model picks from the toolset that was already re-selected for the corrected
    /// intent. A refusal to ask therefore costs a round trip at worst, never a wrong action.
    ///
    /// The rule, explicitly, because it is the one judgement call of this feature:
    ///   - fewer than two candidates    -> no question, because a question offers exactly two options;
    ///   - no previous label in THIS turn's language
    ///                                  -> no question, because rule 1 obliges it to name the
    ///                                     misunderstanding and there is nothing left to name it with;
    ///   - both candidates carry a retrieval score and the gap is at most
    ///     CorrectionAmbiguityTolerance -> ask, the ranking does not separate them;
    ///   - neither carries a score      -> ask, nothing ranks them at all (a keyword guarantee is a
    ///                                     yes/no, not a degree, so two of them are simply tied);
    ///   - exactly one carries a score  -> proceed, because retrieval judged it relevant while the other
    ///                                     is only a literal keyword hit;
    ///   - both scored, gap larger      -> proceed, the ranking already separated them.
    /// A null score is therefore NOT read as zero. Treating it as zero made every pair of keyword
    /// guarantees tie with every unscored recipe step, which asked far more often than rule 2 intends.
    /// The two boundary cases are measured by correction-v1 (cr-de-005-ambiguous, cr-de-007-clear-winner)
    /// before the tolerance is calibrated.
    ///
    /// The three nouns the question puts into its translated frame are AUTHORED labels (AgentSkill.Labels),
    /// resolved for THIS turn's language by SkillLabelResolver - the two options from LLMFunction.Labels of
    /// the re-assembled toolset, the previous action from AssistantLastActionCall.SkillLabels of the
    /// record, because that skill is excluded from this turn's toolset. There is no
    /// English fallback: a language for which no label was authored yields none, and the turn then asks
    /// nothing rather than putting an English noun into a translated sentence (spec §1 rule 4). Until the
    /// 21 language packs ship their own skill-labels.json, that is the state of every plugin language -
    /// the owner-accepted gap this replaced the earlier English-only gate with. The difference is which
    /// way the refusal points: the gate refused every language BUT English, this refuses only what is
    /// genuinely unauthored, and the four core languages are authored in skill-seeds.json.
    ///
    /// No question is asked either when an option cannot be named without leaking an internal snake_case
    /// skill name, when both options would be named identically - two CRUD descriptions can share a first
    /// sentence, and "do you mean X or X?" is a question the user cannot answer - or when the language
    /// has no authored sentence at all.
    /// </summary>
    /// <param name="orderedCandidates">Deterministic candidates, scored ones first, best score first</param>
    /// <param name="previousLabel">
    /// Authored label of what the previous turn did, resolved in THIS turn's language by QuestionLabelOf;
    /// null when the correction's language has no authored label for it
    /// </param>
    /// <param name="language">Active language of the turn the question would be asked in</param>
    internal static string? BuildClarification(
        IReadOnlyList<LLMFunction> orderedCandidates, string? previousLabel, string? language)
    {
        if (orderedCandidates.Count < GracefulCorrectionDefaults.ClarificationCandidateCount
            || string.IsNullOrWhiteSpace(previousLabel))
        {
            return null;
        }

        var best = orderedCandidates[0].RetrievalScore;
        var runnerUp = orderedCandidates[1].RetrievalScore;

        var ambiguous = best.HasValue && runnerUp.HasValue
            ? best.Value - runnerUp.Value <= GracefulCorrectionDefaults.CorrectionAmbiguityTolerance
            : !best.HasValue && !runnerUp.HasValue;

        if (!ambiguous)
        {
            return null;
        }

        var firstLabel = DescribeFunction(orderedCandidates[0], language);
        var secondLabel = DescribeFunction(orderedCandidates[1], language);
        if (firstLabel == null || secondLabel == null
            || string.Equals(firstLabel, secondLabel, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!GracefulCorrectionTexts.TryGetText(
                GracefulCorrectionTexts.ClarificationQuestion, language, out var template))
        {
            return null;
        }

        return template
            .Replace(GracefulCorrectionTexts.PreviousActionPlaceholder, previousLabel, StringComparison.Ordinal)
            .Replace(GracefulCorrectionTexts.FirstOptionPlaceholder, firstLabel, StringComparison.Ordinal)
            .Replace(GracefulCorrectionTexts.SecondOptionPlaceholder, secondLabel, StringComparison.Ordinal);
    }

    /// <summary>
    /// A user-facing label for a candidate in the turn's language: its authored label, capped. Never the
    /// internal snake_case name - InternalIdentifierRedactor exists precisely because those must not
    /// reach a user - and, since 2026-09-16, never the raw skill description either: measured against
    /// skill-seeds.json, 321 of 470 first description sentences are longer than OptionLabelMaxLength and
    /// were being cut mid-sentence, on top of only existing in English. The cap stays as a belt-and-braces
    /// guard for pack-authored labels, which no seed guard can reach.
    /// </summary>
    /// <param name="function">The candidate whose authored label is used as an option label</param>
    /// <param name="language">Active language of the turn the question would be asked in</param>
    internal static string? DescribeFunction(LLMFunction function, string? language) =>
        SkillLabelResolver.Resolve(
            function.Labels, language, GracefulCorrectionDefaults.OptionLabelMaxLength);

    /// <summary>
    /// The whole phrase the note substitutes for its language slot. Never empty: a blank slot would read
    /// as "Answer in ." and the model would fall back to guessing, which is what the one-language rule
    /// exists to prevent. A turn without a language does NOT get a default tag either - ordering English
    /// for a user writing German would break the same rule from the other side - it is pointed at the
    /// user's own message instead. A phrase rather than a bare tag because only the named-tag half reads
    /// correctly in quotes, so the quoting lives here and not in the template.
    /// </summary>
    /// <param name="language">Active language of the turn, or null when the turn carries none</param>
    internal static string AnswerLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language)
            ? GracefulCorrectionNotes.LanguageOfTheUserMessage
            : string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                GracefulCorrectionNotes.NamedLanguageTemplate,
                language);
}
