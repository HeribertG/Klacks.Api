// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using SessionOptions = Microsoft.ML.OnnxRuntime.SessionOptions;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Onnx;

/// <summary>
/// Runtime knobs of the ONNX embedding session: how the session is built, whether the CPU arena is
/// shrunk after a bulk chunk, and how many inference calls may run at the same time. Default since
/// 2026-09-06 (owner decision, option B of docs/handoffs/onnx-entscheidung-2026-09-06.md): arena on
/// via CreateEmbedding, bulk shrink on, gate at the core count - the combination that was stationary
/// in every measurement. The pre-2026-09-06 behaviour is CreateMemoryFrugal, no shrink, no gate.
///
/// 🔴 A replacement session factory MUST keep GraphOptimizationLevel at ORT_ENABLE_BASIC. The fp16
/// export cannot be loaded at ORT_ENABLE_ALL - session construction throws on an inserted
/// precision-free cast in SimplifiedLayerNormFusion. See OnnxSessionOptionsFactory for the
/// measurement. Arena, memory pattern and thread counts are free to vary.
/// </summary>
/// <param name="CreateSessionOptions">Factory for the session options; disposed by the provider after the session is built.</param>
/// <param name="ShrinkArenaAfterBulkRun">Adds the arena-shrinkage run option to the bulk path only.
/// Bulk passes are serial and shape-diverse, so releasing the arena's free regions after every chunk
/// keeps the high-water mark from becoming resident for the process lifetime; the query path never
/// shrinks because concurrent small runs re-request the freed regions immediately. Measured on the
/// bulk path 2026-09-06 (1306 MB resident after a pass instead of 2058, high-water 2070 instead of
/// frugal's 2574 over four passes); the concurrency argument against shrinking the query path was
/// measured on the RERANKER 2026-09-05 and carried over, not re-measured here. EmbedBatchAsync is the
/// bulk path, and EmbedAsync shrinks with it because it delegates there; EmbedQueryAsync never does.</param>
/// <param name="MaxConcurrentRuns">Upper bound on concurrent inference; UnlimitedConcurrency disables the gate.</param>
public sealed record OnnxEmbeddingRuntimeProfile(
    Func<SessionOptions> CreateSessionOptions,
    bool ShrinkArenaAfterBulkRun,
    int MaxConcurrentRuns)
{
    public const int UnlimitedConcurrency = 0;

    // Same reasoning as the reranker gate: a run beyond the core count waits for a core anyway but
    // already holds its activation buffers, so the gate follows the core count (2 in the production
    // container) and grows with the CPU limit.
    public static int DefaultMaxConcurrentRuns => Environment.ProcessorCount;

    public static OnnxEmbeddingRuntimeProfile Default { get; } =
        new(OnnxSessionOptionsFactory.CreateEmbedding,
            ShrinkArenaAfterBulkRun: true,
            MaxConcurrentRuns: DefaultMaxConcurrentRuns);

    public static OnnxEmbeddingRuntimeProfile Legacy { get; } =
        new(OnnxSessionOptionsFactory.CreateMemoryFrugal,
            ShrinkArenaAfterBulkRun: false,
            MaxConcurrentRuns: UnlimitedConcurrency);
}
