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
    /// Resolves the language-pack synonyms for one message language. Case-insensitive on the key so a
    /// casing/culture variant ("ES") still resolves, while preserving region-qualified plugin codes
    /// such as "zh-CN" (compared, not lowercased).
    /// Every consumer goes through this method rather than reading <see cref="Synonyms"/> directly: a
    /// Dictionary lookup uses the dictionary's own ordinal comparer, so a caller that asked for "zh-cn"
    /// silently got no synonyms where production found them. The turn-eval replay did exactly that and
    /// therefore measured a narrower vocabulary than the engine routes on.
    /// </summary>
    public IReadOnlyCollection<string>? SynonymsFor(string? language)
    {
        if (string.IsNullOrEmpty(language) || Synonyms == null)
        {
            return null;
        }

        foreach (var entry in Synonyms)
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
