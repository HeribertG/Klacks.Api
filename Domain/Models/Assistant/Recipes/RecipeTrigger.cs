// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant.Recipes;

public sealed class RecipeTrigger
{
    public List<RecipeCondition> AllOf { get; set; } = new();

    public List<RecipeCondition> NoneOf { get; set; } = new();
}
