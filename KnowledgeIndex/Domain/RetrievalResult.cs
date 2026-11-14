// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.KnowledgeIndex.Domain;

/// <summary>
/// Result of a retrieval call. Empty means the caller should fall back to Tier2 LLM classifier.
/// SkillRerank is set on skill passes only and says how the pass was ordered; it is null for recipe passes and for
/// passes that returned nothing. AssociationPool holds the cosine-scored skill candidates of a vector-order skill pass
/// down to KnowledgeIndexConstants.SkillAssociationPoolSize, beyond the topK cut; the skill-graph association step
/// ranks graph neighbours from it. It is empty on reranked passes, whose scores are on the cross-encoder's scale.
/// </summary>
/// <param name="Candidates">Ranked candidates, highest score first. Empty if none passed cutoff.</param>
public sealed record RetrievalResult(IReadOnlyList<RetrievalCandidate> Candidates)
{
    public bool IsEmpty => Candidates.Count == 0;

    public SkillRerankDecision? SkillRerank { get; init; }

    public IReadOnlyList<RetrievalCandidate> AssociationPool { get; init; } = [];
}
