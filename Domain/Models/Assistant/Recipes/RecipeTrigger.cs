// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Assistant.Recipes;

public sealed class RecipeTrigger
{
    public List<RecipeCondition> AllOf { get; set; } = new();

    public List<RecipeCondition> NoneOf { get; set; } = new();

    /// <summary>
    /// True restricts the semantic fallback to messages that hit one of the recipe's language-pack anchors. Set for a
    /// writing recipe whose trigger is a single phrase list: it has no subject condition the fallback could check, and
    /// without this a similarity score alone reached its confirmation gate (live 2026-10-20, setup-planning-profile).
    /// Null or false keeps the regular subject gate. Nullable so recipes that do not set it serialize without it.
    /// </summary>
    public bool? PackAnchorRequired { get; set; }
}
