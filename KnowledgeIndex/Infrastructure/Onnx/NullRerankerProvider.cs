// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// No-op reranker provider used on platforms where the ONNX Runtime native library is not usable
/// (currently Windows ARM64 / Snapdragon X). Returns zero scores so retrieval keeps its pre-rerank
/// ordering without loading ONNX.
/// </summary>

using Klacks.Api.KnowledgeIndex.Application.Interfaces;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Onnx;

public sealed class NullRerankerProvider : IRerankerProvider
{
    public Task<double[]> ScoreAsync(
        string query,
        IReadOnlyList<string> candidates,
        CancellationToken cancellationToken) =>
        Task.FromResult(new double[candidates.Count]);
}
