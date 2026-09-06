// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Tokenizers.DotNet;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Onnx;

/// <summary>
/// Reranker provider backed by the mmarco-mMiniLMv2-L12-H384-v1 cross-encoder (int8)
/// via ONNX Runtime.
/// Scores query-candidate pairs by concatenating them with the tokenizer's pair encoding,
/// running the classification head, and applying sigmoid to convert logits to 0-1 scores.
/// Holds its session only while it is being used: an idle sweep may release it, and the next call
/// rebuilds it transparently.
/// </summary>
/// <param name="loader">Model loader used to download and cache ONNX model files.</param>
/// <param name="modelDirectory">Local directory where model and tokenizer files are stored.</param>
/// <param name="profile">Session construction, arena shrinkage and concurrency limit; defaults to the shipped behaviour.</param>
public sealed class OnnxRerankerProvider : IRerankerProvider, IUnloadableInferenceSession, IAsyncDisposable
{
    // Everything a run needs, copied out of the fields while the init lock is held. The run works off
    // this value and never reads a field again, which is what makes an unload mid-inference impossible.
    private readonly record struct SessionLease(
        InferenceSession Session, Tokenizer Tokenizer, string[] OutputNames, RunOptions? RunOptions);

    private readonly ModelLoader _loader;
    private readonly string _modelDirectory;
    private readonly OnnxRerankerRuntimeProfile _profile;
    private InferenceSession? _session;
    private Tokenizer? _tokenizer;
    private string[] _outputNames = [];
    private RunOptions? _runOptions;
    private readonly SemaphoreSlim? _gate;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private int _activeRuns;
    private long _lastUsedTimestamp;
    private int _loadCount;
    private int _disposed;

    private const long PadTokenId = 1;

    // XLM-RoBERTa position embeddings allow indices [0, 513] with a padding offset of 2,
    // so each tokenized sequence must be capped at 512 tokens to avoid an out-of-bounds Gather.
    private const int MaxSequenceLength = 512;

    private const string LogitsOutputName = "logits";

    private readonly string _modelUrl;
    private readonly string _modelSha256;
    private readonly string _tokenizerUrl;
    private readonly string _tokenizerSha256;

    /// <param name="modelUrl">Download source; defaults to the configured reranker. Pass an empty
    /// string together with an empty hash to use a model file already present in the directory, which
    /// is how alternative rerankers are benchmarked without touching production configuration.</param>
    public OnnxRerankerProvider(
        ModelLoader loader,
        string modelDirectory,
        string? modelUrl = null,
        string? modelSha256 = null,
        string? tokenizerUrl = null,
        string? tokenizerSha256 = null,
        OnnxRerankerRuntimeProfile? profile = null)
    {
        _loader = loader;
        _modelDirectory = modelDirectory;
        _modelUrl = modelUrl ?? KnowledgeIndexConstants.RerankerModelUrl;
        _modelSha256 = modelSha256 ?? KnowledgeIndexConstants.RerankerModelSha256;
        _tokenizerUrl = tokenizerUrl ?? KnowledgeIndexConstants.RerankerTokenizerUrl;
        _tokenizerSha256 = tokenizerSha256 ?? KnowledgeIndexConstants.RerankerTokenizerSha256;
        _profile = profile ?? OnnxRerankerRuntimeProfile.Default;
        _gate = _profile.MaxConcurrentRuns > OnnxRerankerRuntimeProfile.UnlimitedConcurrency
            ? new SemaphoreSlim(_profile.MaxConcurrentRuns, _profile.MaxConcurrentRuns)
            : null;
    }

    public string SessionName => KnowledgeIndexConstants.RerankerModelName;

    // Read outside the init lock on purpose: this is only ever an optimization that lets the sweep
    // skip a provider that holds nothing, and a stale answer costs at most one wasted try-acquire.
    // Nothing dereferences the session based on this property.
    public bool IsLoaded => _session is not null;

    public int LoadCount => Volatile.Read(ref _loadCount);

    public async Task<double[]> ScoreAsync(string query, IReadOnlyList<string> candidates, CancellationToken ct)
    {
        if (candidates.Count == 0) return [];

        var lease = await AcquireSessionAsync(ct);
        try
        {
            // Every row of a batch is padded to the batch's longest row, so a batch that mixes lengths
            // pays for padding on all the others. The index texts are wildly uneven - measured over
            // knowledge_index on 2026-08-03: 478 rows, median 903 chars, max 3542, which tokenizes to a
            // median of 144 and a maximum above the 512 cap. In arrival order a single long candidate
            // therefore inflated all 16 rows to 512 tokens, and roughly 72% of the compute went into
            // padding. Grouping similar lengths together removes that waste without touching a single
            // score: identical inputs, identical logits, only the batch composition changes.
            var encoded = new long[candidates.Count][];
            for (var i = 0; i < candidates.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                encoded[i] = lease.Tokenizer.Encode(query + " </s></s> " + candidates[i])
                    .Select(id => (long)id)
                    .Take(MaxSequenceLength)
                    .ToArray();
            }

            var order = Enumerable.Range(0, candidates.Count)
                .OrderBy(i => encoded[i].Length)
                .ToArray();

            // Lock order invariant: lease first, gate second. The reverse would hold the only gate slot
            // while a cold session load reads 119 MB off disk.
            if (_gate is not null)
            {
                await _gate.WaitAsync(ct);
            }

            try
            {
                return ScoreInBatches(lease, encoded, order, ct);
            }
            finally
            {
                _gate?.Release();
            }
        }
        finally
        {
            ReleaseSession();
        }
    }

    public async Task<bool> TryUnloadIfIdleAsync(TimeSpan idleFor, CancellationToken cancellationToken)
    {
        // Try-acquire, never block: whoever is building or tearing down the session wins, and the sweep
        // simply looks again on its next tick.
        if (!await _initLock.WaitAsync(0, cancellationToken))
        {
            return false;
        }

        try
        {
            if (_session is null) return false;
            if (Volatile.Read(ref _activeRuns) > 0) return false;
            if (Stopwatch.GetElapsedTime(Volatile.Read(ref _lastUsedTimestamp)) < idleFor) return false;

            _runOptions?.Dispose();
            _runOptions = null;
            _session.Dispose();
            _session = null;
            _tokenizer?.Dispose();
            _tokenizer = null;
            _outputNames = [];
            return true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private double[] ScoreInBatches(in SessionLease lease, long[][] encoded, int[] order, CancellationToken ct)
    {
        var scores = new double[encoded.Length];
        var batchSize = KnowledgeIndexConstants.RerankBatchSize;
        for (var start = 0; start < order.Length; start += batchSize)
        {
            ct.ThrowIfCancellationRequested();
            var end = Math.Min(start + batchSize, order.Length);
            var chunk = new long[end - start][];
            for (var i = start; i < end; i++)
                chunk[i - start] = encoded[order[i]];

            var chunkScores = RunScoreBatch(lease, chunk);
            for (var i = 0; i < chunkScores.Length; i++)
                scores[order[start + i]] = chunkScores[i];
        }

        return scores;
    }

    private static double[] RunScoreBatch(in SessionLease lease, IReadOnlyList<long[]> encoded)
    {
        var maxLen = encoded.Max(e => e.Length);
        var batchSize = encoded.Count;

        var inputIds = new long[batchSize * maxLen];
        var attentionMask = new long[batchSize * maxLen];

        for (var i = 0; i < batchSize; i++)
        {
            var row = encoded[i];
            for (var j = 0; j < row.Length; j++)
            {
                inputIds[i * maxLen + j] = row[j];
                attentionMask[i * maxLen + j] = 1;
            }

            for (var j = row.Length; j < maxLen; j++)
            {
                inputIds[i * maxLen + j] = PadTokenId;
            }
        }

        var dims = new[] { batchSize, maxLen };
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(inputIds, dims)),
            NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(attentionMask, dims))
        };

        using var outputs = lease.RunOptions is null
            ? lease.Session.Run(inputs)
            : lease.Session.Run(inputs, lease.OutputNames, lease.RunOptions);
        var logitsTensor = outputs.First(o => o.Name == LogitsOutputName).AsTensor<float>();

        var scores = new double[batchSize];
        for (var i = 0; i < batchSize; i++)
        {
            var logit = logitsTensor.Rank == 2 ? logitsTensor[i, 0] : logitsTensor[i];
            scores[i] = 1.0 / (1.0 + Math.Exp(-logit));
        }

        return scores;
    }

    private async Task<SessionLease> AcquireSessionAsync(CancellationToken ct)
    {
        // No lock-free fast path here on purpose. TryUnloadIfIdleAsync can dispose the session, and a
        // native InferenceSession freed under a running Run() faults the process rather than throwing.
        // Reading the fields under the lock and handing the caller a value copy is what makes an unload
        // during inference structurally impossible. The uncontended acquire costs ~100 ns against an
        // inference measured in tens of milliseconds - do not reintroduce the shortcut.
        await _initLock.WaitAsync(ct);
        try
        {
            if (_session is null)
            {
                await LoadAsync(ct);
            }

            Interlocked.Increment(ref _activeRuns);
            return new SessionLease(_session!, _tokenizer!, _outputNames, _runOptions);
        }
        finally
        {
            _initLock.Release();
        }
    }

    // Timestamp is written BEFORE the counter drops: the unloader only looks at the timestamp once it
    // has seen the counter at zero, so this ordering can only ever make a session look busier, never
    // idler, than it is.
    private void ReleaseSession()
    {
        Volatile.Write(ref _lastUsedTimestamp, Stopwatch.GetTimestamp());
        Interlocked.Decrement(ref _activeRuns);
    }

    // Callers hold _initLock.
    private async Task LoadAsync(CancellationToken ct)
    {
        var modelPath = Path.Combine(_modelDirectory, KnowledgeIndexConstants.RerankerModelFileName);
        var tokenizerPath = Path.Combine(_modelDirectory, KnowledgeIndexConstants.RerankerTokenizerFileName);

        await _loader.EnsureFileAsync(modelPath, _modelUrl, _modelSha256, ct);
        await _loader.EnsureFileAsync(tokenizerPath, _tokenizerUrl, _tokenizerSha256, ct);

        try
        {
            using var sessionOptions = _profile.CreateSessionOptions();
            _session = new InferenceSession(modelPath, sessionOptions);
            _outputNames = _session.OutputMetadata.Keys.ToArray();
            _tokenizer = new Tokenizer(vocabPath: tokenizerPath);

            if (_profile.ShrinkArenaAfterRun)
            {
                _runOptions = new RunOptions();
                _runOptions.AddRunConfigEntry(
                    OnnxRuntimeConfigKeys.RunEnableMemoryArenaShrinkage,
                    OnnxRuntimeConfigKeys.CpuDeviceZero);
            }
        }
        catch
        {
            // Either every field is valid or none is. A tokenizer that fails to build after the session
            // was created would otherwise leave _session non-null next to a null _tokenizer, and since
            // AcquireSessionAsync only rebuilds when _session is null, every later call would dereference
            // that null - until a sweep happened to unload the half-built session and let it retry.
            _runOptions?.Dispose();
            _runOptions = null;
            _session?.Dispose();
            _session = null;
            _tokenizer?.Dispose();
            _tokenizer = null;
            _outputNames = [];
            throw;
        }

        // Stamped here rather than only on release: a session that has been built but has not yet
        // finished a call would otherwise carry timestamp 0, and Stopwatch.GetElapsedTime(0) measures
        // time since boot - the sweep would read a brand new session as infinitely idle.
        Volatile.Write(ref _lastUsedTimestamp, Stopwatch.GetTimestamp());
        Interlocked.Increment(ref _loadCount);
    }

    public async ValueTask DisposeAsync()
    {
        // The container hands this one instance out under three service types and disposes each of
        // them, so this method runs more than once. That used to be free because every call was a
        // no-op on already-disposed objects; waiting on the init lock is not - SemaphoreSlim.WaitAsync
        // throws ObjectDisposedException - so the second entry has to turn back here.
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        // Taken under the init lock, unlike a plain dispose: it closes the shutdown window in which the
        // idle sweep is mid-unload or a call still holds a lease. Freeing a native InferenceSession
        // while Run() executes on it faults the process instead of throwing.
        await _initLock.WaitAsync();
        try
        {
            _runOptions?.Dispose();
            _runOptions = null;
            _session?.Dispose();
            _session = null;
            _tokenizer?.Dispose();
            _tokenizer = null;
            _outputNames = [];
        }
        finally
        {
            _initLock.Release();
        }

        _gate?.Dispose();
        _initLock.Dispose();
    }
}
