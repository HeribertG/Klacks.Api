// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Guards the recipe confirmation/ask replies produced by a tool-less LLM call. Weak local models
/// sometimes emit tool-call markup or a completion CLAIM ("I updated the address.") instead of the
/// yes/no question the confirmation prompt asked for. This strips any tool markup and, when the reply
/// is not question-shaped, substitutes a deterministic localized confirmation so the user never sees a
/// leaked tool call or a hallucinated success. A well-formed native question passes through untouched.
/// </summary>

using System.Globalization;

namespace Klacks.Api.Domain.Services.Assistant;

public static class RecipeReplyGuard
{
    // Latin '?', CJK fullwidth '？', Arabic '؟' — a yes/no confirmation is question-shaped in every
    // supported script, so their absence is the signal that the model answered with a claim, not a question.
    private static readonly char[] QuestionMarks = { '?', '？', '؟' };

    private static readonly Dictionary<string, string> SingleFrames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["de"] = "Möchtest du diese geführte Aktion starten: „{0}\"? Bitte antworte mit Ja oder Nein.",
        ["en"] = "Do you want to start this guided action: \"{0}\"? Please reply with Yes or No.",
        ["fr"] = "Veux-tu lancer cette action guidée : « {0} » ? Réponds par Oui ou Non.",
        ["it"] = "Vuoi avviare questa azione guidata: «{0}»? Rispondi con Sì o No.",
    };

    private static readonly Dictionary<string, string> AlternativeFrames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["de"] = "Ich habe zwei mögliche Aktionen erkannt: „{0}\" oder „{1}\". Welche meinst du – oder keine?",
        ["en"] = "I found two possible actions: \"{0}\" or \"{1}\". Which do you mean — or neither?",
        ["fr"] = "J'ai identifié deux actions possibles : « {0} » ou « {1} ». Laquelle veux-tu — ou aucune ?",
        ["it"] = "Ho individuato due azioni possibili: «{0}» o «{1}». Quale intendi — o nessuna?",
    };

    /// <summary>
    /// Returns the model's confirmation reply if it is a safe question; otherwise a deterministic
    /// localized confirmation built from the recipe goal (and an alternative goal when two flows matched).
    /// </summary>
    public static string SafeConfirmation(string? modelReply, string goal, string? alternativeGoal, string? language)
    {
        var sanitized = Clean(modelReply);
        return IsQuestionShaped(sanitized)
            ? sanitized
            : DeterministicConfirmation(goal, alternativeGoal, language);
    }

    /// <summary>
    /// Returns the model's ask-step reply if it is a safe question; otherwise the recipe-authored ask
    /// prompt itself, which is already the question to pose for the missing slot.
    /// </summary>
    public static string SafeAsk(string? modelReply, string askPrompt)
    {
        var sanitized = Clean(modelReply);
        return IsQuestionShaped(sanitized) ? sanitized : askPrompt;
    }

    private static string Clean(string? modelReply) =>
        string.IsNullOrEmpty(modelReply) ? string.Empty : ToolCallMarkupSanitizer.Sanitize(modelReply);

    private static bool IsQuestionShaped(string sanitized) =>
        !string.IsNullOrWhiteSpace(sanitized) && sanitized.IndexOfAny(QuestionMarks) >= 0;

    private static string DeterministicConfirmation(string goal, string? alternativeGoal, string? language)
    {
        var lang = NormaliseLanguage(language);
        if (!string.IsNullOrWhiteSpace(alternativeGoal))
        {
            var alt = AlternativeFrames.TryGetValue(lang, out var a) ? a : AlternativeFrames["en"];
            return string.Format(CultureInfo.InvariantCulture, alt, goal, alternativeGoal);
        }

        var single = SingleFrames.TryGetValue(lang, out var s) ? s : SingleFrames["en"];
        return string.Format(CultureInfo.InvariantCulture, single, goal);
    }

    private static string NormaliseLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language) || language.Length < 2)
        {
            return "en";
        }

        var prefix = language[..2].ToLowerInvariant();
        return SingleFrames.ContainsKey(prefix) ? prefix : "en";
    }
}
