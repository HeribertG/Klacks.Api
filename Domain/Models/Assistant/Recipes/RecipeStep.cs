// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant.Recipes;

public sealed class RecipeStep
{
    public string Kind { get; set; } = string.Empty;

    public string? Skill { get; set; }

    public string? Note { get; set; }

    public string? Prompt { get; set; }

    public string? Description { get; set; }

    public string? Slot { get; set; }

    public string? Capture { get; set; }

    public Dictionary<string, string>? Inject { get; set; }

    public string? ExpectPresent { get; set; }

    public string? ExpectAbsent { get; set; }
}
