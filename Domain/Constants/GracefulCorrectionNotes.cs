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
/// Every placeholder is asserted by GracefulCorrectionTextGuardTests: string.Format ignores a surplus
/// argument silently, so a lost placeholder drops a value without any error.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class GracefulCorrectionNotes
{
    /// <summary>
    /// Placeholders: {0} previous skill label, {1} previous arguments, {2} the correction, {3} the
    /// opening sentence, {4} the language tag the whole answer must be written in.
    /// </summary>
    public const string CorrectionContextTemplate =
        "CORRECTION — Your previous turn called {0} with {1}. The user is correcting that, not making " +
        "a new request: \"{2}\" The tools for this turn were re-selected from the corrected intent, and " +
        "that skill is not offered again. Answer in language '{4}'. You MUST open your answer by naming " +
        "the new understanding; start from this sentence and rephrase it if it reads better, but never " +
        "leave it out: \"{3}\" If the correction only changes an argument and the previous skill was in " +
        "fact the right one, say so and ask for the corrected value instead of calling a different tool.";

    /// <summary>Placeholder: {0} a user-facing label for what the previous turn did.</summary>
    public const string OpeningSentenceTemplate =
        "Understood - not {0}, but what you just described.";

    /// <summary>
    /// Placeholders: {0} what the previous turn changed, {1} the inverse skill that undoes it, {2} the
    /// language the question must be written in - the undo question is a question to the user and falls
    /// under the same one-language rule as the clarification.
    /// </summary>
    public const string UndoOfferTemplate =
        " The previous turn already changed something ({0}), and it can be undone with '{1}'. After your " +
        "answer, add exactly ONE short sentence in language '{2}' offering that undo as a yes/no question. " +
        "Offer it once and never as a separate dialogue; if the user says yes, the pending confirmation " +
        "will carry it out.";

    /// <summary>Appended when the corrected intent produced no deterministic candidate at all.</summary>
    public const string NoCandidateSuffix =
        " No tool was guaranteed for the corrected intent, so answer from what you have and ask for the " +
        "missing detail rather than guessing a tool.";
}
