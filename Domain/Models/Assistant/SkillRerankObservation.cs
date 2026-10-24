// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// How the skill pass of a turn was ordered, recorded on the turn's trajectory so the skill rerank threshold can be
/// sharpened from real interactions. The host's own copy of the knowledge index's SkillRerankDecision: the Domain
/// layer does not reference the knowledge-index module.
/// </summary>
/// <param name="Margin">Cosine gap between the first two vector candidates - the pass's certainty</param>
/// <param name="Reranked">True when the cross-encoder ordered the pass, false when the vector order was kept</param>
/// <param name="Explored">True when the decision was flipped at random against the threshold rule</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillRerankObservation(double Margin, bool Reranked, bool Explored);
