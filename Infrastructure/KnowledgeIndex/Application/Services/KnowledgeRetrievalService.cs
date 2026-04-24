// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.KnowledgeIndex.Application.Constants;
using Klacks.Api.Infrastructure.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.Infrastructure.KnowledgeIndex.Domain;

namespace Klacks.Api.Infrastructure.KnowledgeIndex.Application.Services;

/// <summary>
/// Retrieves relevant skills and endpoints from the knowledge index using KNN vector search,
/// skill/endpoint deduplication, cross-encoder reranking, and score cutoff filtering.
/// </summary>
/// <param name="embeddingProvider">Converts the user query into a vector embedding.</param>
/// <param name="rerankerProvider">Cross-encoder that scores query-candidate pairs.</param>
/// <param name="repository">Repository for pgvector KNN search with permission filtering.</param>
public sealed class KnowledgeRetrievalService : IKnowledgeRetrievalService
{
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IRerankerProvider _rerankerProvider;
    private readonly IKnowledgeIndexRepository _repository;

    public KnowledgeRetrievalService(
        IEmbeddingProvider embeddingProvider,
        IRerankerProvider rerankerProvider,
        IKnowledgeIndexRepository repository)
    {
        _embeddingProvider = embeddingProvider;
        _rerankerProvider = rerankerProvider;
        _repository = repository;
    }

    public async Task<RetrievalResult> RetrieveAsync(
        string userQuery,
        IReadOnlyCollection<string> userPermissions,
        bool isAdmin,
        int topK,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
            return new RetrievalResult([]);

        var queryVec = await _embeddingProvider.EmbedQueryAsync(userQuery, cancellationToken);

        var candidates = await _repository.FindNearestAsync(
            queryVec, userPermissions, isAdmin, KnowledgeIndexConstants.KnnTopN, cancellationToken);

        if (candidates.Count == 0)
            return new RetrievalResult([]);

        var wrappedEndpoints = candidates
            .Where(c => c.Kind == KnowledgeEntryKind.Skill && c.ExposedEndpointKey is not null)
            .Select(c => c.ExposedEndpointKey!)
            .ToHashSet();

        var filtered = candidates
            .Where(c => c.Kind == KnowledgeEntryKind.Skill || !wrappedEndpoints.Contains(c.SourceId))
            .ToList();

        if (filtered.Count == 0)
            return new RetrievalResult([]);

        var texts = filtered.Select(f => f.Text).ToList();
        var scores = await _rerankerProvider.ScoreAsync(userQuery, texts, cancellationToken);

        Console.WriteLine($"Q={userQuery} scores: {string.Join(", ", filtered.Zip(scores).Select(p => $"{p.First.SourceId}:{p.Second:F3}"))}");

        var ranked = filtered
            .Zip(scores, (e, s) => new RetrievalCandidate(e, s))
            .Where(c => c.Score >= KnowledgeIndexConstants.DefaultScoreCutoff)
            .OrderByDescending(c => c.Score)
            .Take(topK)
            .ToList();

        return new RetrievalResult(ranked);
    }
}
