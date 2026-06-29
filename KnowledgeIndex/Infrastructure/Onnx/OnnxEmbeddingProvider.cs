// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Tokenizers.DotNet;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Onnx;

/// <summary>
/// Embedding provider backed by multilingual-e5-small via ONNX Runtime.
/// Applies E5's required "passage: " prefix for batch embedding and "query: " for query embedding.
/// Uses Tokenizers.DotNet to load the HuggingFace tokenizer.json (Unigram/XLMRoberta format).
/// </summary>
/// <param name="loader">Model loader used to download and cache ONNX model files.</param>
/// <param name="modelDirectory">Local directory where model and tokenizer files are stored.</param>
public sealed class OnnxEmbeddingProvider : IEmbeddingProvider, IAsyncDisposable
{
    private readonly ModelLoader _loader;
    private readonly string _modelDirectory;
    private InferenceSession? _session;
    private Tokenizer? _tokenizer;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private const int PadTokenId = 1;

    // multilingual-e5-small (XLM-RoBERTa based) supports at most 512 tokens; cap each sequence
    // to avoid an out-of-bounds position-embedding Gather on long inputs.
    private const int MaxSequenceLength = 512;

    public int Dimension => KnowledgeIndexConstants.EmbeddingDimension;

    public OnnxEmbeddingProvider(ModelLoader loader, string modelDirectory)
    {
        _loader = loader;
        _modelDirectory = modelDirectory;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        var result = await EmbedBatchAsync([text], ct);
        return result[0];
    }

    public async Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct)
    {
        await EnsureInitializedAsync(ct);
        if (texts.Count == 0) return [];

        var results = new float[texts.Count][];
        var batchSize = KnowledgeIndexConstants.EmbeddingBatchSize;
        for (var start = 0; start < texts.Count; start += batchSize)
        {
            ct.ThrowIfCancellationRequested();
            var end = Math.Min(start + batchSize, texts.Count);
            var chunk = new string[end - start];
            for (var i = start; i < end; i++)
                chunk[i - start] = "passage: " + texts[i];

            var vectors = RunInference(chunk);
            for (var i = 0; i < vectors.Length; i++)
                results[start + i] = vectors[i];
        }

        return results;
    }

    public async Task<float[]> EmbedQueryAsync(string query, CancellationToken ct)
    {
        await EnsureInitializedAsync(ct);
        return RunInference(["query: " + query])[0];
    }

    private float[][] RunInference(string[] texts)
    {
        var encoded = texts
            .Select(t => _tokenizer!.Encode(t).Select(id => (long)id).Take(MaxSequenceLength).ToArray())
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
            NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(inputIds, dims)),
            NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(attentionMask, dims)),
            NamedOnnxValue.CreateFromTensor("token_type_ids", new DenseTensor<long>(tokenTypeIds, dims))
        };

        using var outputs = _session!.Run(inputs);
        var lastHidden = outputs.First(o => o.Name == "last_hidden_state").AsTensor<float>();

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

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_session is not null) return;
        await _initLock.WaitAsync(ct);
        try
        {
            if (_session is not null) return;

            var modelPath = Path.Combine(_modelDirectory, KnowledgeIndexConstants.EmbeddingModelFileName);
            var tokenizerPath = Path.Combine(_modelDirectory, KnowledgeIndexConstants.EmbeddingTokenizerFileName);

            await _loader.EnsureFileAsync(modelPath, KnowledgeIndexConstants.EmbeddingModelUrl,
                KnowledgeIndexConstants.EmbeddingModelSha256, ct);
            await _loader.EnsureFileAsync(tokenizerPath, KnowledgeIndexConstants.EmbeddingTokenizerUrl,
                KnowledgeIndexConstants.EmbeddingTokenizerSha256, ct);

            using var sessionOptions = OnnxSessionOptionsFactory.CreateMemoryFrugal();
            _session = new InferenceSession(modelPath, sessionOptions);
            _tokenizer = new Tokenizer(vocabPath: tokenizerPath);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _session?.Dispose();
        _initLock.Dispose();
        return ValueTask.CompletedTask;
    }
}
