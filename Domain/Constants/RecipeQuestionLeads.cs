// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sentence openings that mark an utterance as a question rather than a request to act. Every recipe is
/// a guided mutation flow, so a recipe that fires on "Wann lege ich ..." drags a plain question into a
/// confirmation gate. The seeded recipes carry this guard by hand in their noneOf lists; a learned
/// recipe gets it written in for it, because a generator that forgets one lead produces a recipe that
/// misbehaves in exactly the way nobody notices until a user complains.
/// The list covers the four core languages and includes the leads that were missing from the seeded
/// recipes until 2026-08-28 (wann, wo, woher, wohin, wem, wen, wer).
/// Every lead is a whole word, and the TRAILING SPACE is what says so - it is load-bearing data, not
/// formatting. RecipeTriggerMatcher.MatchesStartsWith compiles a space-terminated term to a real word
/// boundary (^term\b), so "wie " vetoes the one-word question "Wie?" as well as "Wie geht das", but
/// still never "Wiederholung". Dropping a trailing space would silently demote that lead to an open
/// stem: "liste" without the space would also veto "Listenbericht der offenen Dienste", a false negative
/// that blocks a learned capability without anyone noticing. Inflected forms a stem used to cover
/// (zeige, welcher, erkläre) are therefore listed as words of their own instead.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class RecipeQuestionLeads
{
    public static readonly IReadOnlyList<string> All =
    [
        "wie ", "was ", "warum ", "wieso ", "weshalb ", "welche ", "welcher ", "welches ", "wann ",
        "wo ", "woher ", "wohin ", "wem ", "wen ", "wer ", "zeig ", "zeige ", "liste ", "erklär ",
        "erkläre ", "gibt es ", "ist ", "sind ",
        "how ", "what ", "why ", "which ", "when ", "where ", "who ", "show ", "list ", "explain ",
        "is there ", "are there ", "is ", "are ",
        "comment ", "quoi ", "pourquoi ", "quel ", "quelle ", "quels ", "quelles ", "quand ", "où ",
        "qui ", "montre ", "explique ",
        "come ", "cosa ", "perché ", "quale ", "quali ", "quando ", "dove ", "chi ", "mostra ",
        "elenca ", "spiega "
    ];
}
