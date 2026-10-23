// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Port for the semantic recipe search: ranks the indexed recipes (and only recipes) against a message and
/// returns the best of them ordered by descending score. Implemented outside the Domain on top of the
/// knowledge index, which the Domain never references. The implementation borrows the database connection of the
/// scope it is resolved from, so RecipeEngineService resolves it per lookup from its own child scope, never through
/// its constructor: the request scope's connection is shared with fire-and-forget tasks of the chat pipeline.
/// </summary>

using Klacks.Api.Domain.Models.Assistant.Recipes;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IRecipeCandidateRetriever
{
    /// <param name="message">Text the recipes are scored against</param>
    /// <param name="topK">How many recipes are returned at most</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<RecipeCandidate>> RetrieveAsync(string message, int topK, CancellationToken cancellationToken);
}
