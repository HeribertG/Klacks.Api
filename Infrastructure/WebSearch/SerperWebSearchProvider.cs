// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Application.DTOs.WebSearch;
using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Infrastructure.WebSearch;

public class SerperWebSearchProvider : IWebSearchProvider
{
    private const string ApiUrl = "https://google.serper.dev/search";
    private const int TimeoutSeconds = 10;

    private readonly string _apiKey;
    private readonly IHttpClientFactory _httpClientFactory;

    public string ProviderName => "serper";

    public SerperWebSearchProvider(string apiKey, IHttpClientFactory httpClientFactory)
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

            var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Add("X-API-KEY", _apiKey);
            request.Content = JsonContent.Create(new { q = query, num = maxResults });

            var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<SerperResponse>(ct);
            if (json?.Organic == null)
            {
                return new WebSearchResult { Success = true };
            }

            return new WebSearchResult
            {
                Success = true,
                Results = json.Organic.Select(o => new WebSearchEntry
                {
                    Title = o.Title ?? string.Empty,
                    Snippet = o.Snippet ?? string.Empty,
                    Url = o.Link ?? string.Empty
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            return new WebSearchResult
            {
                Success = false,
                ErrorMessage = $"Serper search failed: {ex.Message}"
            };
        }
    }

    private sealed class SerperResponse
    {
        [JsonPropertyName("organic")]
        public List<SerperOrganic>? Organic { get; set; }
    }

    private sealed class SerperOrganic
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("snippet")]
        public string? Snippet { get; set; }

        [JsonPropertyName("link")]
        public string? Link { get; set; }
    }
}
