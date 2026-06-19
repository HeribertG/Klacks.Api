// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Discovers additional LLM providers via web search and an LLM extraction step.
/// Degrades gracefully: returns an empty list when web search is not configured or
/// when no enabled LLM model is available to perform the extraction.
/// </summary>

using System.Text;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class ProviderWebDiscovery : IProviderWebDiscovery
{
    private const string SearchQuery =
        "OpenAI compatible LLM API providers base url endpoint chat completions";
    private const int MaxSearchResults = 6;
    private const double ExtractionTemperature = 0.0;
    private const int ExtractionMaxTokens = 1500;

    private const string ExtractionSystemPrompt =
        "You extract OpenAI-compatible LLM API providers from web search results. " +
        "Return ONLY a JSON array. Each item: " +
        "{\"providerId\":\"lowercase-slug\",\"providerName\":\"Display Name\"," +
        "\"baseUrl\":\"https://.../v1/\",\"requiresApiKey\":true}. " +
        "baseUrl must be the OpenAI-compatible REST root that exposes a /models and " +
        "/chat/completions endpoint, ending with a trailing slash. " +
        "Only include providers whose base URL you are confident about. " +
        "If none are found, return [].";

    private readonly IWebSearchProviderFactory _webSearchFactory;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILLMRepository _llmRepository;
    private readonly ILogger<ProviderWebDiscovery> _logger;

    public ProviderWebDiscovery(
        IWebSearchProviderFactory webSearchFactory,
        ILLMProviderFactory providerFactory,
        ILLMRepository llmRepository,
        ILogger<ProviderWebDiscovery> logger)
    {
        _webSearchFactory = webSearchFactory;
        _providerFactory = providerFactory;
        _llmRepository = llmRepository;
        _logger = logger;
    }

    public async Task<List<ProviderCandidateResource>> DiscoverAsync(CancellationToken ct = default)
    {
        var searchProvider = await _webSearchFactory.CreateAsync(ct);
        if (searchProvider == null)
        {
            _logger.LogDebug("Web search is not configured; skipping web provider discovery.");
            return [];
        }

        var searchResult = await searchProvider.SearchAsync(SearchQuery, MaxSearchResults, ct);
        if (!searchResult.Success || searchResult.Results.Count == 0)
        {
            _logger.LogDebug("Web search returned no usable results for provider discovery.");
            return [];
        }

        var (model, llmProvider) = await GetCheapestModelAndProviderAsync();
        if (model == null || llmProvider == null)
        {
            _logger.LogDebug("No enabled LLM model available to extract providers from web results.");
            return [];
        }

        var snippets = new StringBuilder();
        foreach (var entry in searchResult.Results)
        {
            snippets.AppendLine($"Title: {entry.Title}");
            snippets.AppendLine($"Snippet: {entry.Snippet}");
            snippets.AppendLine($"Url: {entry.Url}");
            snippets.AppendLine();
        }

        var request = new LLMProviderRequest
        {
            Message = snippets.ToString(),
            SystemPrompt = ExtractionSystemPrompt,
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = ExtractionTemperature,
            MaxTokens = ExtractionMaxTokens,
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken
        };

        var response = await llmProvider.ProcessAsync(request, ct);
        if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
        {
            _logger.LogDebug("Provider extraction LLM call returned no content.");
            return [];
        }

        return ProviderCandidateParser.Parse(response.Content);
    }

    private async Task<(Domain.Models.Assistant.LLMModel? model, ILLMProvider? provider)> GetCheapestModelAndProviderAsync()
    {
        var models = await _llmRepository.GetModelsAsync(onlyEnabled: true);

        var cheapest = models
            .OrderBy(m => m.CostPerInputToken + m.CostPerOutputToken)
            .FirstOrDefault();

        if (cheapest == null)
        {
            return (null, null);
        }

        var provider = await _providerFactory.GetProviderForModelAsync(cheapest.ModelId);
        return (cheapest, provider);
    }
}
