// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.KnowledgeIndex.Application.Interfaces;

/// <summary>
/// The live margin threshold of the Adaptive skill rerank, shared by every request of the process. Starts at the
/// configured value; the threshold tuner sharpens it from real turns and restores the persisted value after a
/// restart. Set clamps to the bounds in KnowledgeIndexConstants, so no caller can push it outside them.
/// </summary>
public interface ISkillRerankThreshold
{
    /// <summary>True when skill passes run in Adaptive mode, the only mode in which the threshold is used and learned.</summary>
    bool IsLearned { get; }

    /// <summary>Probability of flipping an Adaptive decision at random, the controlled sample the tuner compares.</summary>
    double ExplorationRate { get; }

    /// <summary>The current threshold: a skill pass whose margin lies below it is reranked.</summary>
    double Current { get; }

    /// <summary>Replaces the threshold, clamped to [MinSkillRerankMarginThreshold, MaxSkillRerankMarginThreshold].</summary>
    void Set(double value);
}
