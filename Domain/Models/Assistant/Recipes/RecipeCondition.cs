// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant.Recipes;

public sealed class RecipeCondition
{
    public List<string>? AnyWordStart { get; set; }

    /// <summary>
    /// Optional per-locale word-start stems. Keys are language codes ("de", "en", "fr", ...), values are
    /// stem lists that only fire when the request language matches the key. The request language is the
    /// caller's UI language, not a detected message language, so a German UI sending an Italian message
    /// still evaluates the "de" list.
    /// Used for terms whose meaning is language-specific (e.g. "alle" is a German plural article but
    /// a Finnish/Italian preposition). Complements <see cref="AnyWordStart"/>, which applies to every
    /// language. A condition matches when ANY present list hits, so this is OR'd with AnyWordStart.
    /// </summary>
    public Dictionary<string, List<string>>? AnyWordStartByLocale { get; set; }

    public List<string>? AnySubstring { get; set; }

    public List<string>? StartsWith { get; set; }
}
