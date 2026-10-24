// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One turn as the skill rerank threshold tuner sees it: how certain the skill pass was, which order it used, and
/// whether the turn went well.
/// </summary>
/// <param name="Margin">Cosine gap between the first two vector candidates</param>
/// <param name="Reranked">True when the cross-encoder ordered the pass</param>
/// <param name="Success">True when the turn did what was asked and was not corrected afterwards</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillRerankOutcome(double Margin, bool Reranked, bool Success);
