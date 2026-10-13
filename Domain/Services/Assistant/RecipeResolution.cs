// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The outcome of resolving a message against the recipes: the plan to offer, or none, and - only when none could be offered
/// because every match needs a right the user lacks - the first refused recipe, so the caller can explain it.
/// </summary>
/// <param name="Plan">The plan to offer, or null when nothing matched or every match was refused</param>
/// <param name="Denial">The first recipe refused for missing rights; null whenever a plan is returned</param>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public sealed record RecipeResolution(RecipeExecutionPlan? Plan, RecipeDenial? Denial);
