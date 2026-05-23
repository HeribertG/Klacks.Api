// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Base;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;
using Microsoft.Extensions.Configuration;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Cohere;

public class CohereProvider : BaseHttpProvider
{
    private readonly IConfiguration _configuration;

    private const string ChatEndpoint = "chat";
    private const string EventTypeTextGeneration = "text-generation";
    private const string EventTypeToolCallsGeneration = "tool-calls-generation";
    private const string EventTypeToolCallsChunk = "tool-calls-chunk";

    public override string ProviderId => _providerConfig!.ProviderId;
    public override string ProviderName => _providerConfig!.ProviderName;
    public override bool SupportsStreaming => true;

    public CohereProvider(HttpClient httpClient, ILogger<CohereProvider> logger, IConfiguration configuration)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    protected override void ConfigureHttpClient()
    {
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }
    }

    public override async Task<LLMProviderResponse> ProcessAsync(LLMProviderRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return CreateErrorResponse($"{ProviderName} provider is not enabled");
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            return CreateErrorResponse("The provider for the selected model is not available.");
        }

        try
        {
            var cohereRequest = new CohereRequest
            {
                Model = request.ModelId,
                Message = request.Message,
                ChatHistory = BuildChatHistory(request),
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens
            };

            var cohereResponse = await PostJsonAsync<CohereRequest, CohereResponse>(ChatEndpoint, cohereRequest, cancellationToken);

            if (cohereResponse == null || string.IsNullOrEmpty(cohereResponse.Text))
            {
                return CreateErrorResponse($"Invalid response from {ProviderName}");
            }

            return new LLMProviderResponse
            {
                Content = cohereResponse.Text,
                Success = true,
                Usage = new LLMUsage
                {
                    InputTokens = cohereResponse.Meta?.BilledUnits?.InputTokens ?? 0,
                    OutputTokens = cohereResponse.Meta?.BilledUnits?.OutputTokens ?? 0,
                    Cost = CalculateCost(request, 
                        cohereResponse.Meta?.BilledUnits?.InputTokens ?? 0,
                        cohereResponse.Meta?.BilledUnits?.OutputTokens ?? 0)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {Provider} request", ProviderName);
            return CreateErrorResponse("Internal error processing request");
        }
    }

    public override async IAsyncEnumerable<string> ProcessStreamAsync(
        LLMProviderRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            throw new InvalidOperationException($"{ProviderName} provider is not enabled");

        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("The provider for the selected model is not available.");

        var cohereRequest = new CohereRequest
        {
            Model = request.ModelId,
            Message = request.Message,
            ChatHistory = BuildChatHistory(request),
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            Stream = true
        };

        var hasToolCalls = false;

        await foreach (var rawJson in PostStreamAsync(ChatEndpoint, cohereRequest, cancellationToken))
        {
            CohereStreamEvent? evt;
            try
            {
                evt = JsonSerializer.Deserialize<CohereStreamEvent>(rawJson, GetJsonSerializerOptions());
            }
            catch
            {
                continue;
            }

            if (evt == null) continue;

            switch (evt.EventType)
            {
                case EventTypeTextGeneration when evt.Text != null:
                    yield return evt.Text;
                    break;

                case EventTypeToolCallsGeneration when evt.ToolCalls != null:
                    for (var i = 0; i < evt.ToolCalls.Count; i++)
                    {
                        var tc = evt.ToolCalls[i];
                        var tcJson = JsonSerializer.Serialize(new
                        {
                            toolCall = true,
                            index = i,
                            name = tc.Name,
                            arguments = JsonSerializer.Serialize(tc.Parameters ?? new Dictionary<string, object>(), GetJsonSerializerOptions())
                        }, GetJsonSerializerOptions());
                        yield return $"{LLMStreamingTokens.ToolCallPrefix}{tcJson}";
                    }
                    hasToolCalls = true;
                    break;

                case EventTypeToolCallsChunk when evt.ToolCallDelta != null:
                    if (evt.ToolCallDelta.Name != null)
                    {
                        var deltaJson = JsonSerializer.Serialize(new
                        {
                            toolCall = true,
                            index = evt.ToolCallDelta.Index,
                            name = evt.ToolCallDelta.Name,
                            arguments = evt.ToolCallDelta.Parameters ?? string.Empty
                        }, GetJsonSerializerOptions());
                        yield return $"{LLMStreamingTokens.ToolCallPrefix}{deltaJson}";
                        hasToolCalls = true;
                    }
                    break;
            }
        }

        if (hasToolCalls)
            yield return LLMStreamingTokens.ToolCallEnd;
    }

    private List<CohereChatMessage> BuildChatHistory(LLMProviderRequest request)
    {
        var history = new List<CohereChatMessage>();

        foreach (var msg in request.ConversationHistory)
        {
            history.Add(new CohereChatMessage
            {
                Role = msg.Role == "assistant" ? "CHATBOT" : "USER",
                Message = msg.Content
            });
        }

        return history;
    }

    public override async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_httpClient.BaseAddress!, "models"));
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public override async Task<List<Domain.Models.Assistant.LLMModelDiscovery>?> GetAvailableModelsAsync()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return null;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_httpClient.BaseAddress!, "models"));
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var result = System.Text.Json.JsonSerializer.Deserialize<CohereModelsResponse>(
                json, GetJsonSerializerOptions());

            return result?.Models
                .Where(m => !string.IsNullOrWhiteSpace(m.Name))
                .Select(m => new Domain.Models.Assistant.LLMModelDiscovery(m.Name, m.Name))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch models from Cohere");
            return null;
        }
    }
}