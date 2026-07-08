// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Embedding provider backed by the Gemini API's batchEmbedContents endpoint (gemini-embedding-001).
/// Requests "outputDimensionality" as a best-effort hint, but truncates and L2-re-normalizes the
/// result locally regardless — live testing (2026-07-06) showed the API ignores that hint on
/// batchEmbedContents and always returns the full 3072-dim vector, which the pgvector column (fixed
/// 384 dims) then rejects. gemini-embedding-001 is Matryoshka-trained, so truncating the first N
/// dimensions and re-normalizing is a valid embedding on its own, independent of server behavior.
/// Reuses the "google" LLM provider's API key already configured for chat (via
/// ILlmProviderCredentialReader) instead of a separate app-config secret. Used as a local-development
/// alternative to OnnxEmbeddingProvider on platforms where the ONNX Runtime native library cannot run
/// (Windows ARM64) — production (Hetzner, x64) always uses the ONNX provider and never reaches this.
/// </summary>

using System.Text;
using System.Text.Json;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Api;

public sealed class GeminiEmbeddingProvider : IEmbeddingProvider
{
    private const string ProviderId = "google";
    private const string ModelName = "models/gemini-embedding-001";
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/";
    private const string TaskTypeDocument = "RETRIEVAL_DOCUMENT";
    private const string TaskTypeQuery = "RETRIEVAL_QUERY";
    private const string ApiKeyHeaderName = "x-goog-api-key";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly ILlmProviderCredentialReader _credentialReader;

    public GeminiEmbeddingProvider(HttpClient httpClient, ILlmProviderCredentialReader credentialReader)
    {
        _httpClient = httpClient;
        _credentialReader = credentialReader;
    }

    public int Dimension => KnowledgeIndexConstants.EmbeddingDimension;

    public string EmbeddingSpaceId => $"gemini:{ModelName}@{KnowledgeIndexConstants.EmbeddingDimension}";

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        var result = await EmbedManyAsync([text], TaskTypeDocument, cancellationToken);
        return result[0];
    }

    public Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken) =>
        EmbedManyAsync(texts, TaskTypeDocument, cancellationToken);

    public async Task<float[]> EmbedQueryAsync(string query, CancellationToken cancellationToken)
    {
        var result = await EmbedManyAsync([query], TaskTypeQuery, cancellationToken);
        return result[0];
    }

    private async Task<float[][]> EmbedManyAsync(
        IReadOnlyList<string> texts, string taskType, CancellationToken cancellationToken)
    {
        if (texts.Count == 0)
        {
            return [];
        }

        var apiKey = await _credentialReader.GetApiKeyAsync(ProviderId, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"GeminiEmbeddingProvider is active but no enabled '{ProviderId}' LLM provider with an API key is configured.");
        }

        var result = new float[texts.Count][];
        var batchSize = KnowledgeIndexConstants.EmbeddingBatchSize;
        for (var start = 0; start < texts.Count; start += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var end = Math.Min(start + batchSize, texts.Count);
            var chunk = texts.Skip(start).Take(end - start).ToArray();
            var chunkResult = await EmbedChunkAsync(chunk, taskType, apiKey, cancellationToken);
            Array.Copy(chunkResult, 0, result, start, chunkResult.Length);
        }

        return result;
    }

    private async Task<float[][]> EmbedChunkAsync(
        IReadOnlyList<string> texts, string taskType, string apiKey, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            requests = texts.Select(text => new
            {
                model = ModelName,
                content = new { parts = new[] { new { text } } },
                embedContentConfig = new { taskType, outputDimensionality = Dimension }
            }).ToArray()
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{ModelName}:batchEmbedContents")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        request.Headers.Add(ApiKeyHeaderName, apiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Gemini embeddings API returned {(int)response.StatusCode}: {responseBody}");
        }

        var parsed = JsonSerializer.Deserialize<BatchEmbedResponse>(responseBody, JsonOptions);
        if (parsed?.Embeddings is not { Length: > 0 } embeddings || embeddings.Length != texts.Count)
        {
            throw new InvalidOperationException("Gemini embeddings API returned unexpected data.");
        }

        return embeddings.Select(e => TruncateAndL2Normalize(e.Values)).ToArray();
    }

    // Empirically (2026-07-06, live against the Gemini API), embedContentConfig.outputDimensionality
    // is NOT honored on batchEmbedContents for gemini-embedding-001 — the API returns the full
    // 3072-dim vector regardless, which then fails the pgvector column's fixed 384-dim check
    // ("expected 384 dimensions, not 3072"). gemini-embedding-001 is trained with Matryoshka
    // Representation Learning, so truncating to the first N dimensions and re-normalizing is a
    // valid, meaningful reduced-size embedding on its own — this no longer depends on the server
    // honoring the (still sent, best-effort) outputDimensionality hint.
    private static float[] TruncateAndL2Normalize(float[] vector)
    {
        var truncated = vector.Length > KnowledgeIndexConstants.EmbeddingDimension
            ? vector[..KnowledgeIndexConstants.EmbeddingDimension]
            : vector;

        double sumSquares = 0;
        foreach (var v in truncated)
        {
            sumSquares += (double)v * v;
        }

        var norm = Math.Sqrt(sumSquares);
        if (norm == 0)
        {
            return truncated;
        }

        var normalized = new float[truncated.Length];
        for (var i = 0; i < truncated.Length; i++)
        {
            normalized[i] = (float)(truncated[i] / norm);
        }

        return normalized;
    }

    private sealed class BatchEmbedResponse
    {
        public EmbeddingValues[]? Embeddings { get; set; }
    }

    private sealed class EmbeddingValues
    {
        public float[] Values { get; set; } = [];
    }
}
