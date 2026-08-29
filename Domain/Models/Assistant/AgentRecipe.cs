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
    /// Who created this recipe, see AgentRecipeOrigins. The seed loader only ever rewrites Seed rows,
    /// so a recipe the learning loop composed survives every redeployment even if a later seed
    /// definition happened to use the same name.
    /// </summary>
    public string Origin { get; set; } = AgentRecipeOrigins.Seed;
}
