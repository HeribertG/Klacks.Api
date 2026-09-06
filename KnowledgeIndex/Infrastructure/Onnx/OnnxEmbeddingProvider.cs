// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Tokenizers.DotNet;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Onnx;

/// <summary>
/// Embedding provider backed by multilingual-e5-base (fp16) via ONNX Runtime.
/// Applies E5's required "passage: " prefix for batch embedding and "query: " for query embedding.
/// Uses Tokenizers.DotNet to load the HuggingFace tokenizer.json (Unigram/XLMRoberta format).
/// Holds its session only while it is being used: an idle sweep may release it, and the next call
/// rebuilds it transparently.
/// </summary>
/// <param name="loader">Model loader used to download and cache ONNX model files.</param>
/// <param name="modelDirectory">Local directory where model and tokenizer files are stored.</param>
/// <param name="profile">Session construction, bulk arena shrinkage and concurrency limit; defaults to the shipped behaviour.</param>
public sealed class OnnxEmbeddingProvider : IEmbeddingProvider, IUnloadableInferenceSession, IAsyncDisposable
{
    // Everything a run needs, copied out of the fields while the init lock is held. The run works off
    // this value and never reads a field again, which is what makes an unload mid-inference impossible.
    private readonly record struct SessionLease(
        InferenceSession Session, Tokenizer Tokenizer, string[] OutputNames, RunOptions? RunOptions);

    private readonly ModelLoader _loader;
    private readonly string _modelDirectory;
    private readonly OnnxEmbeddingRuntimeProfile _profile;
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

    private const int PadTokenId = 1;

    private const string InputIdsName = "input_ids";
    private const string AttentionMaskName = "attention_mask";
    private const string TokenTypeIdsName = "token_type_ids";

    private const string LastHiddenStateOutputName = "last_hidden_state";

    // The multilingual-e5 family (XLM-RoBERTa based) supports at most 512 tokens; cap each sequence
    // to avoid an out-of-bounds position-embedding Gather on long inputs.
    private const int MaxSequenceLength = 512;

    public int Dimension => KnowledgeIndexConstants.EmbeddingDimension;

    public string EmbeddingSpaceId => $"onnx:{KnowledgeIndexConstants.EmbeddingModelName}@{KnowledgeIndexConstants.EmbeddingDimension}";

    public string SessionName => KnowledgeIndexConstants.EmbeddingModelName;

    // Read outside the init lock on purpose: this is only ever an optimization that lets the sweep
    // skip a provider that holds nothing, and a stale answer costs at most one wasted try-acquire.
    // Nothing dereferences the session based on this property.
    public bool IsLoaded => _session is not null;

    public int LoadCount => Volatile.Read(ref _loadCount);

    public OnnxEmbeddingProvider(
        ModelLoader loader,
        string modelDirectory,
        OnnxEmbeddingRuntimeProfile? profile = null)
    {
        _loader = loader;
        _modelDirectory = modelDirectory;
        _profile = profile ?? OnnxEmbeddingRuntimeProfile.Default;
        _gate = _profile.MaxConcurrentRuns > OnnxEmbeddingRuntimeProfile.UnlimitedConcurrency
            ? new SemaphoreSlim(_profile.MaxConcurrentRuns, _profile.MaxConcurrentRuns)
            : null;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        var result = await EmbedBatchAsync([text], ct);
        return result[0];
    }

    public async Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct)
    {
        // Checked before the lease is taken, not after: acquiring a lease builds the session, so an
        // empty batch would otherwise rebuild 555 MB of weights and reset the idle clock for nothing.
        if (texts.Count == 0) return [];

        // One lease for the whole chunk loop. While it is held the idle sweep sees a running call and
        // leaves the session alone, so a bulk pass can never have the session pulled out from under it.
        var lease = await AcquireSessionAsync(ct);
        try
        {
            // Lock order invariant: lease first, gate second. The reverse would hold the only gate slot
            // while a cold session load reads 555 MB off disk.
            // The gate is taken once for the whole call, not per batch: a bulk pass therefore holds it
            // for its entire duration and blocks query embedding while it runs. That is deliberate -
            // releasing between batches would let a query slip in beside the bulk activations, which is
            // exactly the concurrent peak the gate exists to prevent - but it is a real latency cost the
            // moment MaxConcurrentRuns is lowered while KnowledgeIndexSynchronizer is running.
            await WaitForSlotAsync(ct);
            try
            {
                var results = new float[texts.Count][];
                var batchSize = KnowledgeIndexConstants.EmbeddingBatchSize;
                for (var start = 0; start < texts.Count; start += batchSize)
                {
                    ct.ThrowIfCancellationRequested();
                    var end = Math.Min(start + batchSize, texts.Count);
                    var chunk = new string[end - start];
                    for (var i = start; i < end; i++)
                        chunk[i - start] = "passage: " + texts[i];

                    var vectors = RunInference(lease, chunk, bulk: true);
                    for (var i = 0; i < vectors.Length; i++)
                        results[start + i] = vectors[i];
                }

                return results;
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

    public async Task<float[]> EmbedQueryAsync(string query, CancellationToken ct)
    {
        var lease = await AcquireSessionAsync(ct);
        try
        {
            // Lock order invariant: lease first, gate second. The reverse would hold the only gate slot
            // while a cold session load reads 555 MB off disk.
            await WaitForSlotAsync(ct);
            try
            {
                return RunInference(lease, ["query: " + query], bulk: false)[0];
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

    private async Task WaitForSlotAsync(CancellationToken ct)
    {
        if (_gate is not null)
        {
            await _gate.WaitAsync(ct);
        }
    }

    /// <param name="bulk">Whether this run belongs to a bulk pass; only those may carry the arena
    /// shrinkage run option, and only when the profile asked for it.</param>
    private float[][] RunInference(in SessionLease lease, string[] texts, bool bulk)
    {
        // Copied out of the lease first: an "in" parameter cannot be captured by a lambda.
        var tokenizer = lease.Tokenizer;
        var encoded = texts
            .Select(t => tokenizer.Encode(t).Select(id => (long)id).Take(MaxSequenceLength).ToArray())
            .ToArray();
        var maxLen = encoded.Max(e => e.Length);
        var batchSize = texts.Length;

        var inputIds = new long[batchSize * maxLen];
        var attentionMask = new long[batchSize * maxLen];
        var tokenTypeIds = new long[batchSize * maxLen];

        for (var i = 0; i < batchSize; i++)
        {
            for (var j = 0; j < encoded[i].Length; j++)
            {
                inputIds[i * maxLen + j] = encoded[i][j];
                attentionMask[i * maxLen + j] = 1;
            }

            for (var j = encoded[i].Length; j < maxLen; j++)
            {
                inputIds[i * maxLen + j] = PadTokenId;
            }
        }

        var dims = new[] { batchSize, maxLen };
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(InputIdsName, new DenseTensor<long>(inputIds, dims)),
            NamedOnnxValue.CreateFromTensor(AttentionMaskName, new DenseTensor<long>(attentionMask, dims))
        };

        // Not every export of the same architecture takes token_type_ids: multilingual-e5-small's
        // ONNX graph declares it, -base's does not, and passing an input the graph never declared
        // fails the whole run with "is not in the metadata". Asking the session what it accepts keeps
        // both working and makes the next model swap a constant change rather than a debugging session.
        if (lease.Session.InputMetadata.ContainsKey(TokenTypeIdsName))
        {
            inputs.Add(NamedOnnxValue.CreateFromTensor(TokenTypeIdsName, new DenseTensor<long>(tokenTypeIds, dims)));
        }

        using var outputs = bulk && lease.RunOptions is not null
            ? lease.Session.Run(inputs, lease.OutputNames, lease.RunOptions)
            : lease.Session.Run(inputs);
        var lastHidden = outputs.First(o => o.Name == LastHiddenStateOutputName).AsTensor<float>();

        var result = new float[batchSize][];
        for (var i = 0; i < batchSize; i++)
        {
            var pooled = new float[Dimension];
            var tokenCount = 0;
            for (var j = 0; j < maxLen; j++)
            {
                if (attentionMask[i * maxLen + j] == 0) continue;
                tokenCount++;
                for (var k = 0; k < Dimension; k++)
                    pooled[k] += lastHidden[i, j, k];
            }

            if (tokenCount > 0)
            {
                var norm = 0.0;
                for (var k = 0; k < Dimension; k++) { pooled[k] /= tokenCount; norm += pooled[k] * pooled[k]; }
                norm = Math.Sqrt(norm);
                if (norm > 1e-12) for (var k = 0; k < Dimension; k++) pooled[k] = (float)(pooled[k] / norm);
            }

            result[i] = pooled;
        }

        return result;
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
        var modelPath = Path.Combine(_modelDirectory, KnowledgeIndexConstants.EmbeddingModelFileName);
        var tokenizerPath = Path.Combine(_modelDirectory, KnowledgeIndexConstants.EmbeddingTokenizerFileName);

        await _loader.EnsureFileAsync(modelPath, KnowledgeIndexConstants.EmbeddingModelUrl,
            KnowledgeIndexConstants.EmbeddingModelSha256, ct);
        await _loader.EnsureFileAsync(tokenizerPath, KnowledgeIndexConstants.EmbeddingTokenizerUrl,
            KnowledgeIndexConstants.EmbeddingTokenizerSha256, ct);

        try
        {
            using var sessionOptions = _profile.CreateSessionOptions();
            _session = new InferenceSession(modelPath, sessionOptions);
            _outputNames = _session.OutputMetadata.Keys.ToArray();
            _tokenizer = new Tokenizer(vocabPath: tokenizerPath);

            if (_profile.ShrinkArenaAfterBulkRun)
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
