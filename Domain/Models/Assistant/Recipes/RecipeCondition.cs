// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant.Recipes;

public sealed class RecipeCondition
{
    public List<string>? AnyWordStart { get; set; }

    /// <summary>
    /// Optional per-locale word-start stems. Keys are detected-language codes ("de", "en", "fr", ...),
    /// values are stem lists that only fire when the message's detected language matches the key.
    /// Used for terms whose meaning is language-specific (e.g. "alle" is a German plural article but
    /// a Finnish/Italian preposition). Complements <see cref="AnyWordStart"/>, which applies to every
    /// language. A condition matches when ANY present list hits, so this is OR'd with AnyWordStart.
    /// </summary>
    public Dictionary<string, List<string>>? AnyWordStartByLocale { get; set; }

    public List<string>? AnySubstring { get; set; }

    public List<string>? StartsWith { get; set; }
}
