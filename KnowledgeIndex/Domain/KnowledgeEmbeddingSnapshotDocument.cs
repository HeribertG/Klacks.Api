// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.KnowledgeIndex.Domain;

public sealed class KnowledgeEmbeddingSnapshotDocument
{
    public int FormatVersion { get; set; }
    public string EmbeddingSpaceId { get; set; } = string.Empty;
    public int Dimension { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? SourceVersion { get; set; }
    public List<KnowledgeEmbeddingSnapshotEntry> Entries { get; set; } = [];
}
