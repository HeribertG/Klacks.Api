// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Definition of a single operator-authored recipe for seeding into the agent_recipe database table.
/// The trigger and steps are serialized to JSONB columns; version drives the insert/update/skip sync.
/// </summary>
/// <param name="Name">Unique recipe identifier (kebab-case)</param>
/// <param name="Goal">Human-readable description of what the recipe achieves</param>
/// <param name="Trigger">Keyword groups that engage the recipe (all-of / none-of)</param>
/// <param name="Steps">Ordered steps: ask / search / guard / mutate / verify</param>

using Klacks.Api.Domain.Models.Assistant.Recipes;

namespace Klacks.Api.Infrastructure.Persistence.Seed.Models;

public class RecipeSeedDefinition
{
    public string Name { get; set; } = string.Empty;

    public string Goal { get; set; } = string.Empty;

    public RecipeTrigger Trigger { get; set; } = new();

    public List<RecipeStep> Steps { get; set; } = new();

    public bool IsEnabled { get; set; } = true;

    public int SortOrder { get; set; }

    public int Version { get; set; } = 1;
}
