// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.KnowledgeIndex.Application.Services;

/// <summary>
/// Whether the cross-encoder orders the skill candidates of a turn. Recipe passes always rerank: their match
/// threshold is calibrated on the cross-encoder's score scale.
/// </summary>
public enum SkillRerankMode
{
    /// <summary>Every skill pass is reranked (the behaviour before 2026-10-24, still the default).</summary>
    Always,

    /// <summary>Reranked only when the vector stage is unsure: the cosine gap between its first two candidates is
    /// below SkillRerankOptions.MarginThreshold. Otherwise the vector order and its cosine scores are kept.</summary>
    Adaptive,

    /// <summary>Never reranked: the vector order and its cosine scores are kept on every skill pass.</summary>
    Never
}
