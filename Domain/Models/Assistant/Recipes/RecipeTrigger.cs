// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Assistant.Recipes;

public sealed class RecipeTrigger
{
    public List<RecipeCondition> AllOf { get; set; } = new();

    public List<RecipeCondition> NoneOf { get; set; } = new();
}
