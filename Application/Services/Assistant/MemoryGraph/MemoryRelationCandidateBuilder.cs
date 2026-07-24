// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure, DB-free scoring of candidate memory-relation edges for a single target memory against a pool
/// of other memories of the same agent: shared tags (>= MinSharedTags) and embedding cosine similarity
/// (>= VectorSimilarityThreshold). When both signals fire for the same peer, the higher-confidence one
/// wins. Deterministic and side-effect free so it is testable without a database.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.MemoryGraph;

public static class MemoryRelationCandidateBuilder
{
    public static IReadOnlyList<(Guid MemoryId, double Confidence, string Provenance)> ComputeCandidates(
        AgentMemory target, IReadOnlyList<AgentMemory> pool)
    {
        var scored = new Dictionary<Guid, (double Confidence, string Provenance)>();

        foreach (var peer in pool)
        {
            if (peer.Id == target.Id)
            {
                continue;
            }

            var best = PickBest(ScoreSharedTags(target, peer), ScoreVectorSimilarity(target, peer));
            if (best.HasValue)
            {
                scored[peer.Id] = best.Value;
            }
        }

        return scored
            .OrderByDescending(kv => kv.Value.Confidence)
            .ThenBy(kv => kv.Key)
            .Take(MemoryGraphConstants.MaxEdgesPerMemory)
            .Select(kv => (kv.Key, kv.Value.Confidence, kv.Value.Provenance))
            .ToList();
    }

    public static double CosineSimilarity(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Count; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return normA == 0 || normB == 0 ? 0 : dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private static (double Confidence, string Provenance)? ScoreSharedTags(AgentMemory a, AgentMemory b)
    {
        var tagsA = a.Tags.Select(t => t.Tag).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (tagsA.Count == 0)
        {
            return null;
        }

        var sharedCount = b.Tags.Select(t => t.Tag).Count(tagsA.Contains);
        if (sharedCount < MemoryGraphConstants.MinSharedTags)
        {
            return null;
        }

        var extraShared = sharedCount - MemoryGraphConstants.MinSharedTags;
        var confidence = Math.Min(
            MemoryGraphConstants.MaxSharedTagsConfidence,
            MemoryGraphConstants.SharedTagsBaseConfidence + (extraShared * MemoryGraphConstants.SharedTagsConfidencePerExtraTag));
        return (confidence, MemoryGraphConstants.SharedTagsProvenance);
    }

    private static (double Confidence, string Provenance)? ScoreVectorSimilarity(AgentMemory a, AgentMemory b)
    {
        if (a.Embedding is not { Length: > 0 } embeddingA
            || b.Embedding is not { Length: > 0 } embeddingB
            || embeddingA.Length != embeddingB.Length)
        {
            return null;
        }

        var similarity = CosineSimilarity(embeddingA, embeddingB);
        return similarity >= MemoryGraphConstants.VectorSimilarityThreshold
            ? (similarity, MemoryGraphConstants.VectorSimilarityProvenance)
            : null;
    }

    private static (double Confidence, string Provenance)? PickBest(
        (double Confidence, string Provenance)? a, (double Confidence, string Provenance)? b)
    {
        if (a == null)
        {
            return b;
        }

        if (b == null)
        {
            return a;
        }

        return a.Value.Confidence >= b.Value.Confidence ? a : b;
    }
}
