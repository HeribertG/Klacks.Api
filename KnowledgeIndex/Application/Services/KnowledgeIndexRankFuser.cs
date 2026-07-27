// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.KnowledgeIndex.Domain;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

/// <summary>
/// Combines the semantic (KNN) and lexical (pg_trgm) candidate lists into one ranked list via
/// Reciprocal Rank Fusion, so entries either channel ranks highly are pulled toward the top without
/// needing the two lists' raw scores (cosine distance vs. trigram similarity) to be on comparable
/// scales.
/// </summary>
public static class KnowledgeIndexRankFuser
{
    /// <param name="semanticRanked">Semantic candidates, best match first.</param>
    /// <param name="lexicalRanked">Lexical candidates, best match first.</param>
    /// <param name="rrfK">Reciprocal Rank Fusion damping constant (see KnowledgeIndexConstants.ReciprocalRankFusionK).</param>
    /// <param name="maxCandidates">Cap on the number of fused entries returned, highest fused score first.</param>
    public static IReadOnlyList<KnowledgeEntry> Fuse(
        IReadOnlyList<KnowledgeEntry> semanticRanked,
        IReadOnlyList<KnowledgeEntry> lexicalRanked,
        int rrfK,
        int maxCandidates)
    {
        var scores = new Dictionary<(KnowledgeEntryKind Kind, string SourceId), double>();
        var entries = new Dictionary<(KnowledgeEntryKind Kind, string SourceId), KnowledgeEntry>();

        AccumulateRanks(semanticRanked, rrfK, scores, entries);
        AccumulateRanks(lexicalRanked, rrfK, scores, entries);

        return scores
            .OrderByDescending(pair => pair.Value)
            .Take(maxCandidates)
            .Select(pair => entries[pair.Key])
            .ToList();
    }

    private static void AccumulateRanks(
        IReadOnlyList<KnowledgeEntry> ranked,
        int rrfK,
        Dictionary<(KnowledgeEntryKind Kind, string SourceId), double> scores,
        Dictionary<(KnowledgeEntryKind Kind, string SourceId), KnowledgeEntry> entries)
    {
        for (var i = 0; i < ranked.Count; i++)
        {
            var entry = ranked[i];
            var key = (entry.Kind, entry.SourceId);
            var rank = i + 1;
            var contribution = 1.0 / (rrfK + rank);

            scores[key] = scores.TryGetValue(key, out var existing) ? existing + contribution : contribution;
            entries.TryAdd(key, entry);
        }
    }
}
