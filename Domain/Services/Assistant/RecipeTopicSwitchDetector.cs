// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides whether a user's reply during a recipe's ask step is an independent question rather than an
/// answer to the pending slot. A recipe's tool-less ask-step call has no skills available, so raw-filling
/// an off-topic question into the slot silences every skill the model could otherwise have used to answer
/// it correctly (live incident: an ERP-import question answered as "not possible" while the ask step for
/// setup-consultation was pending). Precision-biased in the OTHER direction from RecipeCancellationDetector:
/// a false negative here just means one more turn of raw-filling (recoverable), while a false positive
/// hijacks a legitimate slot answer as a topic switch and never fills the slot the recipe is waiting for.
/// A message counts as a topic switch when THREE conditions all hold: (a) some sentence in it opens with
/// an interrogative lead in one of the languages MutationIntentDetector.IsInformationQuestion recognizes
/// (core languages plus any plugin-configured question leads), (b) the message carries more than one word,
/// and (c) the message contains an actual question mark (RecipeReplyGuard.QuestionMarks: Latin/CJK/Arabic).
/// The word-count floor exists because our own recipes hand out single-word chip replies ("show", "list",
/// "none", ...) that collide with English question leads read as bare imperatives; a real independent
/// question is never one word. The question-mark requirement exists because a free-text slot (e.g. a note)
/// can legitimately open with an interrogative-shaped clause without being a question at all ("Was
/// Nachtschichten betrifft: verträgt keine.") — punctuation is what actually distinguishes an interrogative
/// SENTENCE from a statement that merely starts with an interrogative WORD. A false positive here hijacks a
/// legitimate slot answer and never fills the slot the recipe is waiting for, which is strictly worse than
/// the false-negative cost (one more turn of raw-filling, recoverable on the next message) — so all three
/// gates are precision-biased in that direction. Splitting into sentences (not just checking the message's
/// own opening word) is what catches the live repro: "Ich habe eine xml Datei ... Wie kann ich es
/// einbinden?" opens with a plain statement and only its second sentence is the actual question.
/// </summary>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class RecipeTopicSwitchDetector
{
    private static readonly Regex WordPattern = new(@"\p{L}+", RegexOptions.Compiled);
    private static readonly char[] SentenceTerminators = { '.', '!', '?', '\n', ';' };

    private const int MinWordCountForTopicSwitch = 2;

    public static bool IsTopicSwitch(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        if (message.IndexOfAny(RecipeReplyGuard.QuestionMarks) < 0)
        {
            return false;
        }

        if (WordPattern.Matches(message).Count < MinWordCountForTopicSwitch)
        {
            return false;
        }

        foreach (var sentence in message.Split(SentenceTerminators, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = sentence.Trim();
            if (trimmed.Length > 0 && MutationIntentDetector.IsInformationQuestion(trimmed))
            {
                return true;
            }
        }

        return false;
    }
}
