// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.KnowledgeIndex.Domain;

namespace Klacks.Api.KnowledgeIndex.Application.Interfaces;

/// <summary>
/// Exports the current knowledge_index table content into a portable snapshot document, so a fresh
/// database can seed its embeddings from a committed file instead of re-embedding every entry.
/// </summary>
public interface IKnowledgeEmbeddingSnapshotExporter
{
    Task<KnowledgeEmbeddingSnapshotDocument> ExportAsync(CancellationToken ct);
}
