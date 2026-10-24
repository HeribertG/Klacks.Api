// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// How one skill pass was ordered, carried on its RetrievalResult so the turn can record it for the threshold tuner.
/// </summary>
/// <param name="Margin">Cosine gap between the first two vector candidates - the pass's certainty</param>
/// <param name="Reranked">True when the cross-encoder ordered the pass, false when the vector order was kept</param>
/// <param name="Explored">True when the decision was flipped at random against the threshold rule, the controlled
/// sample that lets the tuner compare both orders at the same margin</param>

namespace Klacks.Api.KnowledgeIndex.Domain;

public sealed record SkillRerankDecision(double Margin, bool Reranked, bool Explored);
