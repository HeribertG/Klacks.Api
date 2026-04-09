// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.KnowledgeIndex.Domain;

/// <summary>
/// Result of a retrieval call. Empty means the caller should fall back to Tier2 LLM classifier.
/// </summary>
/// <param name="Candidates">Ranked candidates, highest score first. Empty if none passed cutoff.</param>
public sealed record RetrievalResult(IReadOnlyList<RetrievalCandidate> Candidates)
{
    public bool IsEmpty => Candidates.Count == 0;
}
