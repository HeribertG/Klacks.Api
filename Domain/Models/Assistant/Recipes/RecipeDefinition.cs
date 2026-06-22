// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant.Recipes;

public sealed class RecipeDefinition
{
    public string Name { get; set; } = string.Empty;

    public string Goal { get; set; } = string.Empty;

    public RecipeTrigger Trigger { get; set; } = new();

    public List<RecipeStep> Steps { get; set; } = new();
}
