// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    private readonly ModelLoader _loader;
    private readonly string _modelDirectory;
    private readonly OnnxEmbeddingRuntimeProfile _profile;
    private readonly OnnxSessionHolder<OnnxSessionLease> _holder;

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

    public bool IsLoaded => _holder.IsLoaded;

    public int LoadCount => _holder.LoadCount;

    public OnnxEmbeddingProvider(
        ModelLoader loader,
        string modelDirectory,
        OnnxEmbeddingRuntimeProfile? profile = null)
    {
        _loader = loader;
        _modelDirectory = modelDirectory;
        _profile = profile ?? OnnxEmbeddingRuntimeProfile.Default;
        _holder = new OnnxSessionHolder<OnnxSessionLease>(BuildLeaseAsync, _profile.MaxConcurrentRuns);
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
        var lease = await _holder.AcquireAsync(ct);
        try
        {
            // The gate is taken once for the whole call, not per batch: a bulk pass therefore holds it
            // for its entire duration and blocks query embedding while it runs. That is deliberate -
            // releasing between batches would let a query slip in beside the bulk activations, which is
            // exactly the concurrent peak the gate exists to prevent - but it is a real latency cost the
            // moment MaxConcurrentRuns is lowered while KnowledgeIndexSynchronizer is running.
            await _holder.WaitForSlotAsync(ct);
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
                _holder.ReleaseSlot();
            }
        }
        finally
        {
            _holder.Release();
        }
    }

    public async Task<float[]> EmbedQueryAsync(string query, CancellationToken ct)
    {
        var lease = await _holder.AcquireAsync(ct);
        try
        {
            await _holder.WaitForSlotAsync(ct);
            try
            {
                return RunInference(lease, ["query: " + query], bulk: false)[0];
            }
            finally
            {
                _holder.ReleaseSlot();
            }
        }
        finally
        {
            _holder.Release();
        }
    }

    public Task<bool> TryUnloadIfIdleAsync(TimeSpan idleFor, CancellationToken cancellationToken)
        => _holder.TryUnloadIfIdleAsync(idleFor, cancellationToken);

    /// <param name="bulk">Whether this run belongs to a bulk pass; only those may carry the arena
    /// shrinkage run option, and only when the profile asked for it.</param>
    private float[][] RunInference(in OnnxSessionLease lease, string[] texts, bool bulk)
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

    // Runs under the holder's init lock. Returns a complete lease or throws with nothing left behind:
    // a tokenizer that fails to build after the session was created must not leak that session.
    private async Task<OnnxSessionLease> BuildLeaseAsync(CancellationToken ct)
    {
        var modelPath = Path.Combine(_modelDirectory, KnowledgeIndexConstants.EmbeddingModelFileName);
        var tokenizerPath = Path.Combine(_modelDirectory, KnowledgeIndexConstants.EmbeddingTokenizerFileName);

        await _loader.EnsureFileAsync(modelPath, KnowledgeIndexConstants.EmbeddingModelUrl,
            KnowledgeIndexConstants.EmbeddingModelSha256, ct);
        await _loader.EnsureFileAsync(tokenizerPath, KnowledgeIndexConstants.EmbeddingTokenizerUrl,
            KnowledgeIndexConstants.EmbeddingTokenizerSha256, ct);

        InferenceSession? session = null;
        Tokenizer? tokenizer = null;
        RunOptions? runOptions = null;
        try
        {
            using var sessionOptions = _profile.CreateSessionOptions();
            session = new InferenceSession(modelPath, sessionOptions);
            var outputNames = session.OutputMetadata.Keys.ToArray();
            tokenizer = new Tokenizer(vocabPath: tokenizerPath);

            if (_profile.ShrinkArenaAfterBulkRun)
            {
                runOptions = new RunOptions();
                runOptions.AddRunConfigEntry(
                    OnnxRuntimeConfigKeys.RunEnableMemoryArenaShrinkage,
                    OnnxRuntimeConfigKeys.CpuDeviceZero);
            }

            return new OnnxSessionLease(session, tokenizer, outputNames, runOptions);
        }
        catch
        {
            runOptions?.Dispose();
            session?.Dispose();
            tokenizer?.Dispose();
            throw;
        }
    }

    public ValueTask DisposeAsync() => _holder.DisposeAsync();
}
