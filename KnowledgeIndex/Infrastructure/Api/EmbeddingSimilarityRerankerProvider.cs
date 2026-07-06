// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reranker fallback that scores candidates by cosine similarity to the query embedding instead of
/// a cross-encoder. There is no widely available hosted API equivalent to the ONNX cross-encoder
/// (OnnxRerankerProvider), so this reuses whatever IEmbeddingProvider is already registered — a
/// weaker but real, functioning similarity signal, used only where the ONNX reranker is unavailable.
/// </summary>

using Klacks.Api.KnowledgeIndex.Application.Interfaces;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Api;

public sealed class EmbeddingSimilarityRerankerProvider : IRerankerProvider
{
    private readonly IEmbeddingProvider _embeddingProvider;

    public EmbeddingSimilarityRerankerProvider(IEmbeddingProvider embeddingProvider)
    {
        _embeddingProvider = embeddingProvider;
    }

    public async Task<double[]> ScoreAsync(
        string query, IReadOnlyList<string> candidates, CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var queryVector = await _embeddingProvider.EmbedQueryAsync(query, cancellationToken);
        var candidateVectors = await _embeddingProvider.EmbedBatchAsync(candidates, cancellationToken);

        var scores = new double[candidates.Count];
        for (var i = 0; i < candidateVectors.Length; i++)
        {
            scores[i] = CosineSimilarity(queryVector, candidateVectors[i]);
        }

        return scores;
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
