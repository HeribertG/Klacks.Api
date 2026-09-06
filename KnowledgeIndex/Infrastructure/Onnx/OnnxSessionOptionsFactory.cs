// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.ML.OnnxRuntime;
using SessionOptions = Microsoft.ML.OnnxRuntime.SessionOptions;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Onnx;

/// <summary>
/// Builds the ONNX Runtime session options for the two knowledge-index models. Three profiles, one
/// per measured workload: CreateMemoryFrugal (arena off, single thread - the historical default and
/// the fallback for hosts without headroom), CreateEmbedding (arena on at ORT_ENABLE_BASIC - the
/// embedder since 2026-09-06) and CreateThroughput (arena on at ORT_ENABLE_ALL - the reranker).
///
/// 🔴 The fp16 embedding export loads ONLY at ORT_ENABLE_BASIC. Raising its GraphOptimizationLevel
/// to ORT_ENABLE_ALL made session construction throw outright, on an inserted precision-free cast in
/// SimplifiedLayerNormFusion - reproduced with onnxruntime 1.23.2 (Python) and Microsoft.ML.OnnxRuntime
/// 1.27.1 (.NET), measured 2026-08-20. On 1.29.0 the same file happens to load at ORT_ENABLE_ALL, but
/// the fused graph produces different vectors while EmbeddingSpaceId stays the same, so switching the
/// level silently mixes vector spaces in the index. Treat the level as load-bearing, not as a knob.
/// </summary>
public static class OnnxSessionOptionsFactory
{
    /// <summary>
    /// Arena off, memory pattern off, one thread. Releases activation buffers to the allocator after
    /// every run, which is what kept the startup index build inside the container in 2026. Measured
    /// 2026-09-05/06 against the arena profile below: the freed buffers are not returned to the kernel
    /// but fragment the glibc heap, so resident memory grows under concurrent queries (2683 MB at eight
    /// callers, still rising) and oscillates across bulk passes (1313-1734 MB, high-water 2574 MB).
    /// Kept as the opt-out profile; no production model runs on it since 2026-09-06.
    /// </summary>
    public static SessionOptions CreateMemoryFrugal()
    {
        return new SessionOptions
        {
            EnableCpuMemArena = false,
            EnableMemoryPattern = false,
            ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_BASIC,
            InterOpNumThreads = 1,
            IntraOpNumThreads = 1,
        };
    }

    /// <summary>
    /// Embedding profile: CPU arena and memory pattern on, ORT_ENABLE_BASIC (see the class note), all
    /// cores. Measured 2026-09-05/06 on linux/glibc against CreateMemoryFrugal, vectors bit-identical:
    /// under eight concurrent queries gated to two, resident memory plateaus at ~1810 MB instead of
    /// growing past 2683 MB; on the bulk index build the arena would keep the pass's high-water mark
    /// (2060 MB) resident for the process lifetime, which is why OnnxEmbeddingProvider shrinks the arena
    /// after every bulk chunk (1306 MB after a pass, stationary over four passes). Thread count does not
    /// change resident memory (1809 vs 1823 MB) and halves query latency on two cores.
    /// </summary>
    public static SessionOptions CreateEmbedding()
    {
        return new SessionOptions
        {
            EnableCpuMemArena = true,
            EnableMemoryPattern = true,
            ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_BASIC,
            InterOpNumThreads = 1,
            IntraOpNumThreads = Environment.ProcessorCount,
        };
    }

    /// <summary>
    /// Options for models that only ever run on the interactive request path, where the input is a
    /// handful of short sequences and latency is what the user feels. The frugal profile above pins
    /// inference to a single core, which on the chat path costs whole seconds: measured 2026-08-03,
    /// scoring 25 query-candidate pairs took a median of 21 993 ms frugal versus 7 346 ms here (3.0x)
    /// on a 10-core host.
    /// Applying this to the reranker does NOT reintroduce the startup out-of-memory the frugal profile
    /// exists to prevent: that failure comes from embedding the whole index in one bulk pass, and the
    /// reranker never participates in it. Its only callers are KnowledgeRetrievalService and
    /// RecipeSkillMarginEvaluator, both per-request and both bounded by MaxRerankerCandidates (25), so
    /// the activation tensor stays small no matter how large the index grows.
    /// Do NOT hand this to the embedding provider without re-testing memory: KnowledgeIndexSynchronizer
    /// drives it over every row of the index at startup, which is exactly the unbounded case. Since
    /// the embedder moved to fp16 that is no longer merely risky but fatal - see the note above.
    /// Nor is the reverse a way out for the reranker: measured 2026-08-20, fp32 under the frugal
    /// profile collapses to 0.07 pairs/s (456 s for 32 pairs), so this model has no sparing option.
    /// </summary>
    public static SessionOptions CreateThroughput()
    {
        return new SessionOptions
        {
            EnableCpuMemArena = true,
            EnableMemoryPattern = true,
            ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
            InterOpNumThreads = 1,
            IntraOpNumThreads = Environment.ProcessorCount,
        };
    }
}
