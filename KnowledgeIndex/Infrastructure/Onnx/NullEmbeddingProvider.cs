// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// No-op embedding provider used on platforms where the ONNX Runtime native library is not usable
/// (currently Windows ARM64 / Snapdragon X, where ONNX 1.20.1's bundled cpuinfo fails to detect the
/// SoC and crashes the process on session creation). Returns zero vectors so the knowledge-index
/// sync and chat retrieval degrade gracefully to the Tier2 classifier instead of loading ONNX.
/// </summary>

using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Onnx;

public sealed class NullEmbeddingProvider : IEmbeddingProvider
{
    public int Dimension => KnowledgeIndexConstants.EmbeddingDimension;

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken) =>
        Task.FromResult(new float[Dimension]);

    public Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        var result = new float[texts.Count][];
        for (var i = 0; i < texts.Count; i++)
        {
            result[i] = new float[Dimension];
        }

        return Task.FromResult(result);
    }

    public Task<float[]> EmbedQueryAsync(string query, CancellationToken cancellationToken) =>
        Task.FromResult(new float[Dimension]);
}
