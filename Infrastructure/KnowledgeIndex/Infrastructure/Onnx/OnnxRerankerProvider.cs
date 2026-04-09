// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.KnowledgeIndex.Application.Constants;
using Klacks.Api.Infrastructure.KnowledgeIndex.Application.Interfaces;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Tokenizers.DotNet;

namespace Klacks.Api.Infrastructure.KnowledgeIndex.Infrastructure.Onnx;

/// <summary>
/// Reranker provider backed by mmarco-mMiniLMv2-L12-H384-v1 cross-encoder via ONNX Runtime.
/// Scores query-candidate pairs by concatenating them with the tokenizer's pair encoding,
/// running the classification head, and applying sigmoid to convert logits to 0-1 scores.
/// </summary>
/// <param name="loader">Model loader used to download and cache ONNX model files.</param>
/// <param name="modelDirectory">Local directory where model and tokenizer files are stored.</param>
public sealed class OnnxRerankerProvider : IRerankerProvider, IAsyncDisposable
{
    private readonly ModelLoader _loader;
    private readonly string _modelDirectory;
    private InferenceSession? _session;
    private Tokenizer? _tokenizer;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private const long PadTokenId = 1;

    public OnnxRerankerProvider(ModelLoader loader, string modelDirectory)
    {
        _loader = loader;
        _modelDirectory = modelDirectory;
    }

    public async Task<double[]> ScoreAsync(string query, IReadOnlyList<string> candidates, CancellationToken ct)
    {
        if (candidates.Count == 0) return [];

        await EnsureInitializedAsync(ct);

        var pairs = candidates.Select(c => query + " </s></s> " + c).ToArray();
        var encoded = pairs.Select(p => _tokenizer!.Encode(p).Select(id => (long)id).ToArray()).ToArray();

        var maxLen = encoded.Max(e => e.Length);
        var batchSize = candidates.Count;

        var inputIds = new long[batchSize * maxLen];
        var attentionMask = new long[batchSize * maxLen];

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
            NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(attentionMask, dims))
        };

        using var outputs = _session!.Run(inputs);
        var logitsTensor = outputs.First(o => o.Name == "logits").AsTensor<float>();

        var scores = new double[batchSize];
        for (var i = 0; i < batchSize; i++)
        {
            var logit = logitsTensor.Rank == 2 ? logitsTensor[i, 0] : logitsTensor[i];
            scores[i] = 1.0 / (1.0 + Math.Exp(-logit));
        }

        return scores;
    }

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_session is not null) return;
        await _initLock.WaitAsync(ct);
        try
        {
            if (_session is not null) return;

            var modelPath = Path.Combine(_modelDirectory, KnowledgeIndexConstants.RerankerModelFileName);
            var tokenizerPath = Path.Combine(_modelDirectory, KnowledgeIndexConstants.RerankerTokenizerFileName);

            await _loader.EnsureFileAsync(modelPath, KnowledgeIndexConstants.RerankerModelUrl,
                KnowledgeIndexConstants.RerankerModelSha256, ct);
            await _loader.EnsureFileAsync(tokenizerPath, KnowledgeIndexConstants.RerankerTokenizerUrl,
                KnowledgeIndexConstants.RerankerTokenizerSha256, ct);

            _session = new InferenceSession(modelPath);
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
