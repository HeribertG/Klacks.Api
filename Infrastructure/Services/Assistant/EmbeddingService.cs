using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class EmbeddingService : IEmbeddingService
{
    private readonly ILogger<EmbeddingService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    private const string DefaultModel = "text-embedding-3-small";
    private const int DefaultDimensions = 1536;

    public EmbeddingService(
        ILogger<EmbeddingService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public bool IsAvailable
    {
        get
        {
            var apiKey = _configuration["LLM:Embedding:ApiKey"]
                ?? _configuration["LLM:OpenAI:ApiKey"];
            return !string.IsNullOrWhiteSpace(apiKey);
        }
    }

    public async Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = _configuration["LLM:Embedding:ApiKey"]
                ?? _configuration["LLM:OpenAI:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogDebug("No embedding API key configured, skipping embedding generation");
                return null;
            }

            var baseUrl = _configuration["LLM:Embedding:BaseUrl"] ?? "https://api.openai.com/v1";
            var model = _configuration["LLM:Embedding:Model"] ?? DefaultModel;

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                input = text,
                model,
                dimensions = DefaultDimensions
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{baseUrl}/embeddings", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Embedding API returned {StatusCode}: {Error}", response.StatusCode, errorContent);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var embeddingResponse = JsonSerializer.Deserialize<EmbeddingApiResponse>(responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (embeddingResponse?.Data is not { Length: > 0 })
            {
                _logger.LogWarning("Embedding API returned empty data");
                return null;
            }

            return embeddingResponse.Data[0].Embedding;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate embedding for text");
            return null;
        }
    }

    private class EmbeddingApiResponse
    {
        public EmbeddingData[]? Data { get; set; }
    }

    private class EmbeddingData
    {
        public float[] Embedding { get; set; } = [];
    }
}
