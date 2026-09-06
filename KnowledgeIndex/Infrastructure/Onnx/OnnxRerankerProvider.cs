// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    private readonly ModelLoader _loader;
    private readonly string _modelDirectory;
    private readonly OnnxRerankerRuntimeProfile _profile;
    private readonly OnnxSessionHolder<OnnxSessionLease> _holder;

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
        _holder = new OnnxSessionHolder<OnnxSessionLease>(BuildLeaseAsync, _profile.MaxConcurrentRuns);
    }

    public string SessionName => KnowledgeIndexConstants.RerankerModelName;

    public bool IsLoaded => _holder.IsLoaded;

    public int LoadCount => _holder.LoadCount;

    public async Task EnsureLoadedAsync(CancellationToken ct)
    {
        await _holder.AcquireAsync(ct);
        _holder.Release();
    }

    public async Task<double[]> ScoreAsync(string query, IReadOnlyList<string> candidates, CancellationToken ct)
    {
        if (candidates.Count == 0) return [];

        var lease = await _holder.AcquireAsync(ct);
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

            await _holder.WaitForSlotAsync(ct);
            try
            {
                return ScoreInBatches(lease, encoded, order, ct);
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

    private double[] ScoreInBatches(in OnnxSessionLease lease, long[][] encoded, int[] order, CancellationToken ct)
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

    private static double[] RunScoreBatch(in OnnxSessionLease lease, IReadOnlyList<long[]> encoded)
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

    // Runs under the holder's init lock. Returns a complete lease or throws with nothing left behind:
    // a tokenizer that fails to build after the session was created must not leak that session.
    private async Task<OnnxSessionLease> BuildLeaseAsync(CancellationToken ct)
    {
        var modelPath = Path.Combine(_modelDirectory, KnowledgeIndexConstants.RerankerModelFileName);
        var tokenizerPath = Path.Combine(_modelDirectory, KnowledgeIndexConstants.RerankerTokenizerFileName);

        await _loader.EnsureFileAsync(modelPath, _modelUrl, _modelSha256, ct);
        await _loader.EnsureFileAsync(tokenizerPath, _tokenizerUrl, _tokenizerSha256, ct);

        InferenceSession? session = null;
        Tokenizer? tokenizer = null;
        RunOptions? runOptions = null;
        try
        {
            using var sessionOptions = _profile.CreateSessionOptions();
            session = new InferenceSession(modelPath, sessionOptions);
            var outputNames = session.OutputMetadata.Keys.ToArray();
            tokenizer = new Tokenizer(vocabPath: tokenizerPath);

            if (_profile.ShrinkArenaAfterRun)
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
