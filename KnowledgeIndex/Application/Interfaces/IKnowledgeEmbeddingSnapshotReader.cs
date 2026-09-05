// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.KnowledgeIndex.Application.Interfaces;

public interface IKnowledgeEmbeddingSnapshotReader
{
    /// <summary>
    /// Returns the shipped embedding vectors keyed by lowercase hex text hash. Yields an empty
    /// dictionary when the snapshot is disabled, missing, unparsable, or was produced for a different
    /// embedding space, format version or dimension — the caller then falls back to embedding.
    /// </summary>
    /// <param name="embeddingSpaceId">Embedding space the caller's vectors must belong to.</param>
    /// <param name="dimension">Vector length the caller expects.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyDictionary<string, float[]>> LoadAsync(
        string embeddingSpaceId,
        int dimension,
        CancellationToken ct);
}
