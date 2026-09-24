// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.ObjectModel;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public class AgentRecipe : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Goal { get; set; } = string.Empty;

    public Dictionary<string, string>? GoalTranslations { get; set; }

    public string TriggerJson { get; set; } = "{}";

    public string StepsJson { get; set; } = "[]";

    public bool IsEnabled { get; set; } = true;

    public int SortOrder { get; set; }

    public int Version { get; set; } = 1;

    public Dictionary<string, List<string>>? Synonyms { get; set; }

    /// <summary>
    /// Per-language exclusion vocabulary contributed by the language packs, keyed the same way as
    /// <see cref="Synonyms"/>. It lives in its own column rather than merged into <see cref="TriggerJson"/>
    /// because a seed version bump rewrites TriggerJson wholesale - anything merged in there would be
    /// deleted on the next redeployment. RecipeSeedLoader therefore never touches this property.
    /// </summary>
    public Dictionary<string, List<string>>? Vetoes { get; set; }

    /// <summary>
    /// Resolves the language-pack synonyms for one message language. Case-insensitive on the key so a
    /// casing/culture variant ("ES") still resolves, while preserving region-qualified plugin codes
    /// such as "zh-CN" (compared, not lowercased).
    /// Every consumer goes through this method rather than reading <see cref="Synonyms"/> directly: a
    /// Dictionary lookup uses the dictionary's own ordinal comparer, so a caller that asked for "zh-cn"
    /// silently got no synonyms where production found them. The turn-eval replay did exactly that and
    /// therefore measured a narrower vocabulary than the engine routes on.
    /// </summary>
    public IReadOnlyCollection<string>? SynonymsFor(string? language) => ResolveForLanguage(Synonyms, language);

    /// <summary>
    /// The <see cref="Vetoes"/> counterpart of <see cref="SynonymsFor"/>, sharing its lookup so the two
    /// columns cannot drift apart on key casing or region-qualified codes. Kept beside it rather than as
    /// a second implementation: the ordinal-comparer divergence the summary above describes was itself a
    /// duplicated lookup, and a second copy of the fix is a second place to forget the next one.
    /// </summary>
    public IReadOnlyCollection<string>? VetoesFor(string? language) => ResolveForLanguage(Vetoes, language);

    /// <summary>
    /// Per-language subject vocabulary contributed by the language packs (recipe-anchors.json), keyed in
    /// manifest spelling ("zh-CN") like <see cref="Vetoes"/>. It is the plugin-language counterpart of
    /// the non-verb allOf conditions, which only carry de/en/fr/it: the semantic fallback accepts an
    /// embedding hit when the message names the recipe's subject in the core trigger OR in any installed
    /// pack. Own column for the same reason as <see cref="Vetoes"/>; RecipeSeedLoader never touches it.
    /// </summary>
    public Dictionary<string, List<string>>? Anchors { get; set; }

    /// <summary>
    /// The <see cref="Anchors"/> counterpart of <see cref="VetoesFor"/>, sharing the same lookup.
    /// </summary>
    public IReadOnlyCollection<string>? AnchorsFor(string? language) => ResolveForLanguage(Anchors, language);

    /// <summary>
    /// The anchors of every installed pack language, never null. A method rather than a property so EF
    /// never tries to map it. The semantic anchor evaluates the union of all of them because the request
    /// language is the UI language, not the message language.
    /// </summary>
    public IReadOnlyDictionary<string, List<string>> AllAnchors() =>
        Anchors ?? (IReadOnlyDictionary<string, List<string>>)ReadOnlyDictionary<string, List<string>>.Empty;

    /// <summary>
    /// The pack vetoes of every installed language as one distinct list, never null - the veto counterpart
    /// of <see cref="AllAnchors"/>. The semantic fallback must veto with the same scope it anchors with:
    /// anchoring on every pack while vetoing only with the UI-language pack let a Spanish question from a
    /// German UI pass the Spanish anchor and reach the confirmation gate. The keyword path keeps
    /// <see cref="VetoesFor"/>. Distinct is case-insensitive like the matcher; a trailing space is kept,
    /// because it marks a whole word rather than an open stem.
    /// </summary>
    public IReadOnlyCollection<string> AllVetoTerms() =>
        Vetoes == null
            ? []
            : Vetoes.Values
                .Where(terms => terms != null)
                .SelectMany(terms => terms)
                .Where(term => !string.IsNullOrWhiteSpace(term))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

    private static IReadOnlyCollection<string>? ResolveForLanguage(
        Dictionary<string, List<string>>? byLanguage, string? language)
    {
        if (string.IsNullOrEmpty(language) || byLanguage == null)
        {
            return null;
        }

        foreach (var entry in byLanguage)
        {
            if (string.Equals(entry.Key, language, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Who created this recipe, see AgentRecipeOrigins. The seed loader only ever rewrites Seed rows,
    /// so a recipe the learning loop composed survives every redeployment even if a later seed
    /// definition happened to use the same name.
    /// </summary>
    public string Origin { get; set; } = AgentRecipeOrigins.Seed;
}
