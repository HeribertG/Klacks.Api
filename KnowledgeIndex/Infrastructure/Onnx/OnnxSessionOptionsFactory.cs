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
}
