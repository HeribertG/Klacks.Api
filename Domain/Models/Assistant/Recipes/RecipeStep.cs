// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Assistant.Recipes;

public sealed class RecipeStep
{
    public string Kind { get; set; } = string.Empty;

    public string? Skill { get; set; }

    public string? Note { get; set; }

    public string? Prompt { get; set; }

    public Dictionary<string, string>? PromptTranslations { get; set; }

    public string? Description { get; set; }

    public string? Slot { get; set; }

    public string? Capture { get; set; }

    public Dictionary<string, string>? Inject { get; set; }
}
