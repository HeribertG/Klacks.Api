// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One recipe the semantic search ranked against a message: the recipe name it points at and its score.
/// </summary>
/// <param name="RecipeName">Name of the ranked recipe, matched case-insensitively against the enabled recipes</param>
/// <param name="Score">Similarity score of the recipe for the message; higher is better</param>

namespace Klacks.Api.Domain.Models.Assistant.Recipes;

public sealed record RecipeCandidate(string RecipeName, double Score);
