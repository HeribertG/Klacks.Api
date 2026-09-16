// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The volatile system note of a correction turn. English and model-facing, exactly like
/// RecipeEngineDefaults.AskStepInstructionTemplate: Klacks has no per-language mechanism for
/// server-authored model instructions, and the rendering is done by the model. The language is named
/// EXPLICITLY ({4}) rather than as "the user's language" - exactly one language is active per user
/// (owner rule, spec §1 rule 4), and a model that has to infer it from a short correction like
/// "Nein, nicht die Kunden" gets it wrong.
/// The note does three things at once, and all three are load-bearing:
/// (1) it states what the previous turn did, so the model can name it;
/// (2) it hands over a PRE-FORMULATED opening sentence, so rule 1 ("no silent re-routing") does not
///     depend on the model inventing one - the model may rephrase it, never drop it;
/// (3) it explicitly permits calling the excluded skill again with corrected arguments, because a
///     correction that only concerns the arguments is TP3 and must not be forced into a wrong skill.
/// Every placeholder will be asserted by GracefulCorrectionTextGuardTests (Task 5a): string.Format
/// ignores a surplus argument silently, so a lost placeholder drops a value without any error.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class GracefulCorrectionNotes
{
    /// <summary>
    /// Placeholders: {0} previous skill label, {1} previous arguments, {2} the correction, {3} the
    /// opening sentence, {4} a full PHRASE naming the language the whole answer must be written in.
    /// {4} is a phrase rather than a bare tag because it has two shapes - a named tag
    /// (NamedLanguageTemplate) and the fallback that points at the user's own message
    /// (LanguageOfTheUserMessage) - and only one of them reads correctly inside quotes. The template
    /// therefore adds no quotes of its own; the phrase brings them where they belong.
    /// </summary>
    public const string CorrectionContextTemplate =
        "CORRECTION — Your previous turn called {0} with {1}. The user is correcting that, not making " +
        "a new request: \"{2}\" The tools for this turn were re-selected from the corrected intent, and " +
        "that skill is not offered again. Answer in {4}. You MUST open your answer by naming " +
        "the new understanding; start from this sentence, translated into {4}, and rephrase it if it " +
        "reads better, but never leave it out: \"{3}\" If the correction only changes an argument and the " +
        "previous skill was in fact the right one, say so and ask for the corrected value instead of " +
        "calling a different tool.";

    /// <summary>Placeholder: {0} a user-facing label for what the previous turn did.</summary>
    public const string OpeningSentenceTemplate =
        "Understood - not {0}, but what you just described.";

    /// <summary>
    /// Stands in for the previous action when the turn that made the call captured no display label.
    /// English like the rest of this file, because it is substituted into a MODEL-facing note:
    /// MutationGuardConstants.RedactedInternalIdentifier is the user-facing redaction and is German, so
    /// putting it here would drop a German fragment into an English instruction and invite the model to
    /// answer in the wrong language. The internal snake_case name is never an option either.
    /// MODEL-FACING ONLY. It reaches a user solely as whatever the model makes of it while writing its
    /// own answer, never verbatim in server-authored user-facing text: the clarification question
    /// refuses to be built at all when this stand-in is the only label available (TurnPreparationService
    /// .BuildClarification), because a question carrying an English "the tool it used" would name nothing
    /// and break the one-language rule at the same time.
    /// </summary>
    public const string UnnamedPreviousActionLabel = "the tool it used";

    /// <summary>
    /// Substituted for the language phrase when the turn carries no language. NOT a default tag:
    /// ordering "Answer in the language 'en'" for a user who wrote German would break the one-language
    /// rule the explicit tag exists to enforce, and an installation without a configured language is
    /// exactly the case where the user's own message is the better evidence.
    /// </summary>
    public const string LanguageOfTheUserMessage = "the same language as the user's message";

    /// <summary>
    /// The other shape of the language phrase: a tag the turn actually carries. Quoted here rather than
    /// in the note template, because the fallback above is prose and must NOT be quoted.
    /// Placeholder: {0} the language tag.
    /// </summary>
    public const string NamedLanguageTemplate = "the language '{0}'";

    /// <summary>
    /// Placeholders: {0} what the previous turn changed, {1} a full PHRASE naming the language the
    /// question must be written in - the undo question is a question to the user and falls under the same
    /// one-language rule as the clarification. {1} takes the same value as {4} of
    /// CorrectionContextTemplate (CorrectionOutcomeComposer.AnswerLanguage) and for the same reason
    /// carries its own quotes, so this template adds none.
    /// The inverse SKILL is deliberately NOT named: it is an internal snake_case identifier, the sentence
    /// asked for here is user-facing, and the same composer refuses to leak such a name anywhere else.
    /// The model does not need it either - the pending confirmation token carries the invocation.
    /// </summary>
    public const string UndoOfferTemplate =
        " The previous turn already changed something ({0}), and it can be undone. After your " +
        "answer, add exactly ONE short sentence in {1} offering that undo as a yes/no question. " +
        "Offer it once and never as a separate dialogue; if the user says yes, the pending confirmation " +
        "will carry it out.";

    /// <summary>Appended when the corrected intent produced no deterministic candidate at all.</summary>
    public const string NoCandidateSuffix =
        " No tool was guaranteed for the corrected intent, so answer from what you have and ask for the " +
        "missing detail rather than guessing a tool.";
}
