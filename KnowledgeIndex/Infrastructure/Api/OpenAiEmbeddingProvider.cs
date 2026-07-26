// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Embedding provider backed by the OpenAI embeddings endpoint (text-embedding-3-small). The model is
/// Matryoshka-trained and honours the "dimensions" request parameter, so the pgvector column's fixed
/// size is requested directly instead of truncating a larger vector; the result is still truncated and
/// L2-re-normalized defensively, which is also what OpenAI recommends for reduced dimensions.
/// Reuses the "openai" LLM provider's API key already configured for chat (via
/// ILlmProviderCredentialReader) instead of a separate app-config secret. Used as a local-development
/// alternative to OnnxEmbeddingProvider on platforms where the ONNX Runtime native library cannot run
/// (Windows ARM64) — production (Hetzner, x64) always uses the ONNX provider and never reaches this.
/// </summary>

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Api;

public sealed class OpenAiEmbeddingProvider : IEmbeddingProvider
{
    private const string ProviderId = "openai";
    private const string ModelName = "text-embedding-3-small";
    private const string BaseUrl = "https://api.openai.com/v1/";
    private const string EmbeddingsPath = "embeddings";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly ILlmProviderCredentialReader _credentialReader;

    public OpenAiEmbeddingProvider(HttpClient httpClient, ILlmProviderCredentialReader credentialReader)
    {
        _httpClient = httpClient;
        _credentialReader = credentialReader;
    }

    public int Dimension => KnowledgeIndexConstants.EmbeddingDimension;

    public string EmbeddingSpaceId => $"openai:{ModelName}@{KnowledgeIndexConstants.EmbeddingDimension}";

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        var result = await EmbedManyAsync([text], cancellationToken);
        return result[0];
    }

    public Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken) =>
        EmbedManyAsync(texts, cancellationToken);

    public async Task<float[]> EmbedQueryAsync(string query, CancellationToken cancellationToken)
    {
        var result = await EmbedManyAsync([query], cancellationToken);
        return result[0];
    }

    private async Task<float[][]> EmbedManyAsync(
        IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        if (texts.Count == 0)
        {
            return [];
        }

        var apiKey = await _credentialReader.GetApiKeyAsync(ProviderId, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"OpenAiEmbeddingProvider is active but no enabled '{ProviderId}' LLM provider with an API key is configured.");
        }

        var result = new float[texts.Count][];
        var batchSize = KnowledgeIndexConstants.EmbeddingBatchSize;
        for (var start = 0; start < texts.Count; start += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var end = Math.Min(start + batchSize, texts.Count);
            var chunk = texts.Skip(start).Take(end - start).ToArray();
            var chunkResult = await EmbedChunkAsync(chunk, apiKey, cancellationToken);
            Array.Copy(chunkResult, 0, result, start, chunkResult.Length);
        }

        return result;
    }

    private async Task<float[][]> EmbedChunkAsync(
        IReadOnlyList<string> texts, string apiKey, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = ModelName,
            input = texts,
            dimensions = Dimension
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{EmbeddingsPath}")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI embeddings API returned {(int)response.StatusCode}: {responseBody}");
        }

        var parsed = JsonSerializer.Deserialize<EmbeddingResponse>(responseBody, JsonOptions);
        if (parsed?.Data is not { Count: > 0 } data || data.Count != texts.Count)
        {
            throw new InvalidOperationException("OpenAI embeddings API returned unexpected data.");
        }

        // The API documents the results as index-ordered, but ordering by index makes that explicit:
        // a silently reordered batch would pair every text with the wrong vector.
        return data
            .OrderBy(d => d.Index)
            .Select(d => TruncateAndL2Normalize(d.Embedding))
            .ToArray();
    }

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

    private sealed class EmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingItem>? Data { get; set; }
    }

    private sealed class EmbeddingItem
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = [];
    }
}
