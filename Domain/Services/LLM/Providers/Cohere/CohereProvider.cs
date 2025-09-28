using Klacks.Api.Domain.Services.LLM.Providers.Base;
using Klacks.Api.Presentation.DTOs.LLM;
using Microsoft.Extensions.Configuration;

namespace Klacks.Api.Domain.Services.LLM.Providers.Cohere;

public class CohereProvider : BaseHttpProvider
{
    private readonly IConfiguration _configuration;

    public override string ProviderId => _providerConfig?.ProviderId ?? "cohere";
    public override string ProviderName => _providerConfig?.ProviderName ?? "Cohere";

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

    public override async Task<LLMProviderResponse> ProcessAsync(LLMProviderRequest request)
    {
        if (!IsEnabled)
        {
            return CreateErrorResponse($"{ProviderName} provider is not enabled");
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            return CreateErrorResponse("Der Provider für das gewählte Modell ist nicht verfügbar.");
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

            var cohereResponse = await PostJsonAsync<CohereRequest, CohereResponse>("chat", cohereRequest);

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
            var testClient = new HttpClient();
            testClient.BaseAddress = _httpClient.BaseAddress;
            testClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            
            var response = await testClient.GetAsync("models");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}