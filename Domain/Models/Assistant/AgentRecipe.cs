// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
