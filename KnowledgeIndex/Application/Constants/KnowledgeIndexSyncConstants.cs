// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Constants of the background knowledge index synchronization.
/// </summary>
namespace Klacks.Api.KnowledgeIndex.Application.Constants;

public static class KnowledgeIndexSyncConstants
{
    public const string StartupReason = "application startup";

    public const int PersistChunkSize = KnowledgeIndexConstants.EmbeddingBatchSize * 4;

    // Share of the current skills and recipes that must already have a stored row for retrieval to see
    // the catalogue. Below it (first start, or a first start that was interrupted) startup still runs the
    // sync in the background, but warns that skill retrieval is degraded until it completes.
    public const double MinimumCoverageForFullRetrieval = 0.9;
}
