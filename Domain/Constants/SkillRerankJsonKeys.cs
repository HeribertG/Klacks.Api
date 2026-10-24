// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Keys of the skill rerank observation inside skill_selection_trajectories.knowledge_index_candidates_json, shared
/// by the writer (TrajectoryCaptureService) and the reader (SkillRerankThresholdTuner) so the two cannot drift.
/// </summary>
public static class SkillRerankJsonKeys
{
    public const string Margin = "rerankMargin";
    public const string Reranked = "reranked";
    public const string Explored = "rerankExplored";
}
