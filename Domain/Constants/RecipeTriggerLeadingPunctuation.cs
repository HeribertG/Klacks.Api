// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Characters RecipeTriggerMatcher skips at the opening of a message before it compares startsWith terms
/// (recipe noneOf/allOf startsWith and the language-pack question-word vetoes). Without this a Spanish
/// "¿Cómo añado …?" escapes the veto "cómo ", and a message opened by a quote, bracket or dash escapes
/// every question lead. Covered: Spanish inverted marks, straight and typographic quotes (incl. German
/// low-9, guillemets, CJK corner brackets), opening brackets (incl. full-width) and the hyphen/en/em dash.
/// Skipped together with any whitespace, so French « Wie » with a (narrow) no-break space is covered too.
/// A set rather than "every non-letter" on purpose: only characters that can open a sentence are listed,
/// and no startsWith term may begin with one of them (guarded by tests), since such a term could never
/// match once the character is skipped.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class RecipeTriggerLeadingPunctuation
{
    public const string Characters = "¿¡\"'«»‹›„‚“”‘’([{（【「『-–—";
}
