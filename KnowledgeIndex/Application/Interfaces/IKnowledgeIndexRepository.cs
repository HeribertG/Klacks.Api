// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.KnowledgeIndex.Domain;

namespace Klacks.Api.KnowledgeIndex.Application.Interfaces;

public interface IKnowledgeIndexRepository
{
    Task<IReadOnlyDictionary<(KnowledgeEntryKind Kind, string SourceId), byte[]>> GetAllHashesAsync(CancellationToken ct);
    Task UpsertAsync(IReadOnlyList<KnowledgeEntry> entries, CancellationToken ct);
    Task DeleteAsync(IReadOnlyList<(KnowledgeEntryKind Kind, string SourceId)> keys, CancellationToken ct);

    Task<IReadOnlyList<KnowledgeEntry>> FindNearestAsync(
        float[] queryEmbedding,
        IReadOnlyCollection<string> userPermissions,
        bool adminBypass,
        int topN,
        CancellationToken ct);

    /// <summary>
    /// Lexical (pg_trgm) candidate search: ranks entries by trigram word_similarity of the raw query
    /// against the indexed text, with a similarity() tiebreak for determinism. Language-agnostic (no
    /// stemming, no per-locale text-search configuration), which suits the multilingual index — unlike
    /// tsvector's plainto_tsquery, it needs no language detection and still matches across word-form
    /// variations via character trigram overlap. Complements FindNearestAsync's semantic KNN: exact or
    /// near-exact word matches that the embedding space blurs together (e.g. "absence" vs
    /// "absence-types") resurface here even when they rank outside the semantic top-N.
    /// </summary>
    Task<IReadOnlyList<KnowledgeEntry>> FindLexicalAsync(
        string query,
        IReadOnlyCollection<string> userPermissions,
        bool adminBypass,
        int topN,
        CancellationToken ct);

    /// <summary>
    /// Fetches specific entries by their (kind, source id) keys, ignoring KNN ranking and permission
    /// filtering. Used to force-include a known set of candidates (e.g. a recipe's served skills) so they
    /// carry a comparable index text/score even when a vector search would not surface them.
    /// </summary>
    Task<IReadOnlyList<KnowledgeEntry>> GetByKeysAsync(
        IReadOnlyList<(KnowledgeEntryKind Kind, string SourceId)> keys,
        CancellationToken ct);
}
