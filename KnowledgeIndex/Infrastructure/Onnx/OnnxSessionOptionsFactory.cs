// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.ML.OnnxRuntime;
using SessionOptions = Microsoft.ML.OnnxRuntime.SessionOptions;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Onnx;

/// <summary>
/// Builds memory-frugal ONNX Runtime session options for running the embedding and reranker
/// models inside a memory-capped container. The default CPU arena and memory-pattern optimizer
/// reserve and retain buffers sized to the largest inference run, which spikes resident memory
/// past the container limit during startup index building; disabling them plus pinning
/// single-threaded sequential execution keeps the peak bounded and releases memory between runs.
/// </summary>
public static class OnnxSessionOptionsFactory
{
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
    /// drives it over every row of the index at startup, which is exactly the unbounded case.
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
