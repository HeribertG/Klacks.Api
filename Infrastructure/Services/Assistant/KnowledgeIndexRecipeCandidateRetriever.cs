// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Knowledge-index adapter for IRecipeCandidateRetriever: one retrieval pass restricted to recipe entries, without
/// a permission filter or route boost, mapped to the Domain's RecipeCandidate in the order the index ranked them.
/// The kind filter is applied inside the retrieval before topK, so topK recipes come back rather than the best
/// topK of every kind filtered down afterwards.
/// </summary>
/// <param name="retrieval">Knowledge-index retrieval; resolved from the caller's scope, whose database connection it borrows</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant.Recipes;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Domain;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public sealed class KnowledgeIndexRecipeCandidateRetriever : IRecipeCandidateRetriever
{
    private readonly IKnowledgeRetrievalService _retrieval;

    public KnowledgeIndexRecipeCandidateRetriever(IKnowledgeRetrievalService retrieval)
    {
        _retrieval = retrieval;
    }

    public async Task<IReadOnlyList<RecipeCandidate>> RetrieveAsync(
        string message, int topK, CancellationToken cancellationToken,
        IReadOnlyCollection<string>? admissibleRecipeNames = null)
    {
        var result = await _retrieval.RetrieveAsync(
            message, [], isAdmin: false, topK, currentRoute: null, cancellationToken,
            KnowledgeEntryKind.Recipe, admissibleRecipeNames);

        return result.Candidates
            .Select(candidate => new RecipeCandidate(candidate.Entry.SourceId, candidate.Score))
            .ToList();
    }
}
