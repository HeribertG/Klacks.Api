// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Application.DTOs.WebSearch;
using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Infrastructure.WebSearch;

public class TavilyWebSearchProvider : IWebSearchProvider
{
    private const string ApiUrl = "https://api.tavily.com/search";
    private const int TimeoutSeconds = 15;

    private readonly string _apiKey;
    private readonly IHttpClientFactory _httpClientFactory;

    public string ProviderName => "tavily";

    public TavilyWebSearchProvider(string apiKey, IHttpClientFactory httpClientFactory)
    {
        _apiKey = apiKey;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<WebSearchResult> SearchAsync(
        string query,
        int maxResults = 5,
        CancellationToken ct = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

            var requestBody = new
            {
                api_key = _apiKey,
                query,
                max_results = maxResults,
                include_answer = false
            };

            var response = await client.PostAsJsonAsync(ApiUrl, requestBody, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<TavilyResponse>(ct);
            if (json?.Results == null)
            {
                return new WebSearchResult { Success = true };
            }

            return new WebSearchResult
            {
                Success = true,
                Results = json.Results.Select(r => new WebSearchEntry
                {
                    Title = r.Title ?? string.Empty,
                    Snippet = r.Content ?? string.Empty,
                    Url = r.Url ?? string.Empty
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            return new WebSearchResult
            {
                Success = false,
                ErrorMessage = $"Tavily search failed: {ex.Message}"
            };
        }
    }

    private sealed class TavilyResponse
    {
        [JsonPropertyName("results")]
        public List<TavilyResult>? Results { get; set; }
    }

    private sealed class TavilyResult
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
