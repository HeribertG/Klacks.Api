// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant.Recipes;

public sealed class RecipeCondition
{
    public List<string>? AnyWordStart { get; set; }

    public List<string>? AnySubstring { get; set; }

    public List<string>? StartsWith { get; set; }
}
