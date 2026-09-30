// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Constants of the background knowledge index synchronization.
/// </summary>
namespace Klacks.Api.KnowledgeIndex.Application.Constants;

public static class KnowledgeIndexSyncConstants
{
    public const string StartupReason = "application startup";

    public const int PersistChunkSize = KnowledgeIndexConstants.EmbeddingBatchSize * 4;

    // Share of the current skills and recipes that must already have a stored row before startup lets
    // the sync run in the background. Below it (first start, or a first start that was interrupted)
    // retrieval would miss most of the catalogue, so host start waits instead.
    public const double MinimumCoverageForBackgroundSync = 0.9;
}
