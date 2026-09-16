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
    /// The language the skill descriptions the option labels are taken from are authored in. Not a
    /// fallback and not a default: it is a statement of fact about the seed data, and the interim gate in
    /// BuildClarification exists only as long as it stays true.
    /// </summary>
    private const string SkillDescriptionLanguage = "en";

    /// <summary>
    /// Shortest run of letters that may precede a sentence terminator for it to end a sentence. Two, so
    /// that the last piece of an abbreviation ("e.g.", "z.B.") is not read as the end of the sentence it
    /// sits inside.
    /// </summary>
    private const int MinimumWordLengthBeforeSentenceEnd = 2;

    private static readonly char[] SentenceTerminators = ['.', '!', '?'];

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
        var previousLabel = LabelOf(correctedCall);
        var previousArguments = correctedCall?.ArgumentsJson ?? GracefulCorrectionDefaults.EmptyJsonObject;

        var candidates = DeterministicCandidates(assembledFunctions)
            .OrderByDescending(f => f.RetrievalScore.HasValue)
            .ThenByDescending(f => f.RetrievalScore ?? 0.0)
            .ToList();

        var clarification = BuildClarification(candidates, previousLabel, language);

        var noteLabel = previousLabel ?? GracefulCorrectionNotes.UnnamedPreviousActionLabel;

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
    /// A label for what a recorded call did, or null when the turn that made the call captured none. Never
    /// the internal snake_case name: the label was captured from the toolset of the turn that made the
    /// call (AssistantLastActionCall), because by the time the correction turn runs that skill is excluded
    /// from the toolset and cannot be looked up any more. Null rather than the stand-in, so that each
    /// caller decides for itself what a missing label means - the note substitutes an English
    /// model-facing stand-in, the question refuses to be asked at all.
    /// </summary>
    /// <param name="call">The recorded call of the previous turn, or null when it made none</param>
    internal static string? LabelOf(AssistantLastActionCall? call) =>
        string.IsNullOrWhiteSpace(call?.SkillDisplayLabel) ? null : call!.SkillDisplayLabel!;

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
    ///   - no captured previous label   -> no question, because rule 1 obliges it to name the
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
    /// INTERIM RESTRICTION (pending the owner's decision): the question is asked ONLY when the turn's
    /// base language is English. Its frame is translated into all 25 languages, but the three nouns it
    /// puts inside that frame - the previous action and the two options - are skill descriptions, and
    /// those exist in English only. A German frame around English nouns satisfies rule 4 in form and
    /// breaks it in substance, so outside English the turn fails CLOSED and proceeds without asking; a
    /// turn carrying no language at all cannot be shown to be English and is treated the same way. The
    /// recorded options for lifting this: use the per-language skill synonyms as labels, ship per-pack
    /// skill descriptions, or carve the labels out of rule 4 explicitly. The 25 authored sentences and
    /// the pack loader stay in place because whichever option wins will use them.
    ///
    /// No question is asked either when an option cannot be named without leaking an internal snake_case
    /// skill name, when both options would be named identically - two CRUD descriptions can share a first
    /// sentence, and "do you mean X or X?" is a question the user cannot answer - or when the language
    /// has no authored sentence at all.
    /// </summary>
    /// <param name="orderedCandidates">Deterministic candidates, scored ones first, best score first</param>
    /// <param name="previousLabel">User-facing label of what the previous turn did, null when none was captured</param>
    /// <param name="language">Active language of the turn the question would be asked in</param>
    internal static string? BuildClarification(
        IReadOnlyList<LLMFunction> orderedCandidates, string? previousLabel, string? language)
    {
        if (orderedCandidates.Count < GracefulCorrectionDefaults.ClarificationCandidateCount
            || string.IsNullOrWhiteSpace(previousLabel)
            || !string.Equals(
                LanguageTag.BaseLanguage(language), SkillDescriptionLanguage, StringComparison.OrdinalIgnoreCase))
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

        var firstLabel = DescribeFunction(orderedCandidates[0]);
        var secondLabel = DescribeFunction(orderedCandidates[1]);
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
    /// A user-facing label for a candidate: the first sentence of its description, capped. Never the
    /// internal snake_case name - InternalIdentifierRedactor exists precisely because those must not
    /// reach a user. Null when the skill carries no description.
    /// </summary>
    /// <param name="function">The candidate whose description is turned into an option label</param>
    internal static string? DescribeFunction(LLMFunction function) =>
        FirstSentenceLabel(function.Description, GracefulCorrectionDefaults.OptionLabelMaxLength);

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

    /// <summary>
    /// The shared shape of both user-facing labels: the first sentence of a skill description, trimmed
    /// and capped. The two callers differ only in their cap - the stored label of a recorded call is
    /// sized like the answer excerpt, an option label like the question it has to fit into - so the cap
    /// is a parameter rather than a second copy of this code.
    ///
    /// A sentence ends at '.', '!' or '?' that whitespace or the end of the text follows AND that a word
    /// of at least MinimumWordLengthBeforeSentenceEnd letters precedes. Both halves are needed: without
    /// the first, "e.g" ends the label after five characters; without the second, "Adds e.g. contracts"
    /// still ends it at "Adds e.g." because that dot is followed by a space. Not a sentence splitter -
    /// "etc. and so on" and "No. 5" are still cut, and an ordinal like "3. Schritt" is not; the point is
    /// that the common abbreviations in a skill description no longer truncate its label to a stump.
    /// </summary>
    /// <param name="description">Skill description the label is taken from</param>
    /// <param name="maxLength">Maximum number of characters the label may have</param>
    internal static string? FirstSentenceLabel(string? description, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var sentenceEnd = FirstSentenceEnd(description);
        var label = (sentenceEnd > 0 ? description[..sentenceEnd] : description).Trim();

        return label.Length <= maxLength ? label : label[..maxLength].TrimEnd();
    }

    private static int FirstSentenceEnd(string description)
    {
        for (var index = 0; index < description.Length; index++)
        {
            if (Array.IndexOf(SentenceTerminators, description[index]) < 0
                || !FollowedByWhitespaceOrEnd(description, index)
                || WordLengthBefore(description, index) < MinimumWordLengthBeforeSentenceEnd)
            {
                continue;
            }

            return index;
        }

        return -1;
    }

    private static bool FollowedByWhitespaceOrEnd(string description, int index) =>
        index + 1 >= description.Length || char.IsWhiteSpace(description[index + 1]);

    private static int WordLengthBefore(string description, int index)
    {
        var length = 0;
        for (var cursor = index - 1; cursor >= 0 && char.IsLetter(description[cursor]); cursor--)
        {
            length++;
        }

        return length;
    }
}
