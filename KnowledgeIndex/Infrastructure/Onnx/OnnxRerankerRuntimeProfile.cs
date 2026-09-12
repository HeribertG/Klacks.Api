// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using SessionOptions = Microsoft.ML.OnnxRuntime.SessionOptions;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Onnx;

/// <summary>
/// Runtime knobs of the ONNX reranker session that do not change a single score: how the session is
/// built, whether the CPU arena is shrunk after every run, and how many ScoreAsync calls may run
/// inference at the same time. Default reproduces the behaviour shipped before this record existed.
/// </summary>
/// <param name="CreateSessionOptions">Factory for the session options; disposed by the provider after the session is built.</param>
/// <param name="ShrinkArenaAfterRun">Adds the arena-shrinkage run option to every inference run.</param>
/// <param name="MaxConcurrentRuns">Upper bound on concurrent inference; UnlimitedConcurrency disables the gate.</param>
public sealed record OnnxRerankerRuntimeProfile(
    Func<SessionOptions> CreateSessionOptions,
    bool ShrinkArenaAfterRun,
    int MaxConcurrentRuns)
{
    public const int UnlimitedConcurrency = 0;

    // Measured 2026-09-05 (linux-arm64, ORT 1.29, int8 model, 2 threads, 8 parallel callers): peak
    // resident memory is driven by how many ScoreAsync calls run at once, not by session options -
    // ungated 1057 MB, gate 2 -> 847 MB, gate 1 -> 771 MB, scores identical. Arena shrinkage and a
    // disabled arena both made it worse under concurrency, and the arena keeps its high-water mark,
    // so one burst raises resident memory for the lifetime of the process.
    // Concurrency only buys wall-clock time while cores are free; a run beyond the core count waits
    // for a core anyway but already holds its activation buffers. The gate therefore follows the
    // core count: 3 in the production container (cpus: 3.0 sets ProcessorCount 3) and it
    // grows with the CPU limit. Owner decision 2026-09-05; see
    // docs/knowledge/onnx-reranker-memory-probe-2026-09-05.md.
    public static int DefaultMaxConcurrentRuns => Environment.ProcessorCount;

    /// <summary>
    /// Used wherever no profile is injected (profile == null). It does not know the configured value of
    /// KnowledgeIndex:OnnxAllowIntraOpSpinning and always builds the measured default - only the
    /// composition root passes the configured one through, via ForIntraOpSpinning below.
    /// </summary>
    public static OnnxRerankerRuntimeProfile Default { get; } =
        new(OnnxSessionOptionsFactory.CreateThroughput, ShrinkArenaAfterRun: false, MaxConcurrentRuns: DefaultMaxConcurrentRuns);

    /// <summary>
    /// The default profile with the intra-op spin-wait explicitly set. Used by the composition root so
    /// the deployment can override the measured default without a code change; every other knob stays
    /// exactly as in Default, and neither setting changes a score.
    /// </summary>
    /// <param name="allowIntraOpSpinning">True lets intra-op workers busy-wait between graph nodes</param>
    public static OnnxRerankerRuntimeProfile ForIntraOpSpinning(bool allowIntraOpSpinning) =>
        Default with { CreateSessionOptions = () => OnnxSessionOptionsFactory.CreateThroughput(allowIntraOpSpinning) };
}
