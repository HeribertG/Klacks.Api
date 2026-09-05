// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.KnowledgeIndex.Domain;

public sealed class KnowledgeEmbeddingSnapshotEntry
{
    public short Kind { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public string TextHash { get; set; } = string.Empty;
    public string Embedding { get; set; } = string.Empty;
}
