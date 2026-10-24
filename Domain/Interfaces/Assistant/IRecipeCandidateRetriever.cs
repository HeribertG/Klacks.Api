// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Port for the semantic recipe search: ranks the indexed recipes (and only recipes) against a message and
/// returns the best of them ordered by descending score. Implemented outside the Domain on top of the
/// knowledge index, which the Domain never references. The implementation borrows the database connection of the
/// scope it is resolved from, so resolve it from the scope the caller's other database reads run in:
/// RecipeEngineService keeps all its reads in one child scope of its own, so the KNN query shares that scope's
/// connection instead of the request's.
/// </summary>

using Klacks.Api.Domain.Models.Assistant.Recipes;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IRecipeCandidateRetriever
{
    /// <param name="message">Text the recipes are scored against</param>
    /// <param name="topK">How many recipes are returned at most</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="admissibleRecipeNames">Only these recipes are scored (the rest cost no rerank time); an empty
    /// collection returns nothing without any model call. Null scores every recipe.</param>
    Task<IReadOnlyList<RecipeCandidate>> RetrieveAsync(
        string message, int topK, CancellationToken cancellationToken,
        IReadOnlyCollection<string>? admissibleRecipeNames = null);
}
