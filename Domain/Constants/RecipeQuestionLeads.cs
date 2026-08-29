// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sentence openings that mark an utterance as a question rather than a request to act. Every recipe is
/// a guided mutation flow, so a recipe that fires on "Wann lege ich ..." drags a plain question into a
/// confirmation gate. The seeded recipes carry this guard by hand in their noneOf lists; a learned
/// recipe gets it written in for it, because a generator that forgets one lead produces a recipe that
/// misbehaves in exactly the way nobody notices until a user complains.
/// The list covers the four core languages and includes the leads that were missing from the seeded
/// recipes until 2026-08-28 (wann, wo, woher, wohin, wem, wen, wer).
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class RecipeQuestionLeads
{
    public static readonly IReadOnlyList<string> All =
    [
        "wie ", "was ", "warum ", "wieso ", "weshalb ", "welche", "wann ", "wo ", "woher ", "wohin ",
        "wem ", "wen ", "wer ", "zeig", "liste", "erklär", "gibt es", "ist ", "sind ",
        "how ", "what ", "why ", "which ", "when ", "where ", "who ", "show ", "list ", "explain ",
        "is there", "are there", "is ", "are ",
        "comment ", "quoi ", "pourquoi ", "quel", "quand ", "où ", "qui ", "montre", "liste ", "explique",
        "come ", "cosa ", "perché ", "quale", "quando ", "dove ", "chi ", "mostra", "elenca", "spiega"
    ];
}
