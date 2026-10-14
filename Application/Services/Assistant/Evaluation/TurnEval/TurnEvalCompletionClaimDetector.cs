// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Best-effort detector for an answer that reports a state change as done ("Erledigt, ich habe es geändert").
/// On a turn that made no tool call such a report is invented. The phrase lists cover de, en, fr and it and are a
/// lexical lower bound: a completion claim worded differently, or in another language, is not recognised, and a
/// phrase can also appear inside a refusal ("done" is matched as a whole word only), which is why the caller
/// ignores the result when a refusal was recognised.
/// </summary>
/// <param name="answer">The assistant's final text of the replayed turn</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public static class TurnEvalCompletionClaimDetector
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    private static readonly string[] Phrases =
    [
        "erledigt",
        "habe ich geändert",
        "habe ich angepasst",
        "habe ich gelöscht",
        "habe ich angelegt",
        "habe ich umgestellt",
        "habe ich gesetzt",
        "wurde geändert",
        "wurde angepasst",
        "wurde gelöscht",
        "wurde angelegt",
        "ist jetzt geändert",
        "done",
        "successfully",
        "i have changed",
        "i've changed",
        "i have updated",
        "i've updated",
        "i have deleted",
        "i've deleted",
        "i have created",
        "i've created",
        "has been changed",
        "has been updated",
        "has been deleted",
        "has been created",
        "c'est fait",
        "j'ai modifié",
        "j'ai supprimé",
        "j'ai créé",
        "j'ai mis à jour",
        "a été modifié",
        "a été supprimé",
        "a été créé",
        "fatto",
        "ho modificato",
        "ho eliminato",
        "ho creato",
        "ho aggiornato",
        "è stato modificato",
        "è stato eliminato",
        "è stato creato"
    ];

    private static readonly Regex PhrasePattern = new(
        @"(?<![\p{L}\p{N}])(" + string.Join("|", Phrases.Select(Regex.Escape)) + @")(?![\p{L}\p{N}])",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        MatchTimeout);

    public static bool ClaimsCompletion(string? answer) =>
        !string.IsNullOrWhiteSpace(answer) && PhrasePattern.IsMatch(answer.Replace('’', '\''));
}
