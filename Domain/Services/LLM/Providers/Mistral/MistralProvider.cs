using Klacks.Api.Domain.Services.LLM.Providers.Base;
using Klacks.Api.Domain.Services.LLM.Providers.Shared;
using Klacks.Api.Presentation.DTOs.LLM;

namespace Klacks.Api.Domain.Services.LLM.Providers.Mistral;

public class MistralProvider : BaseOpenAICompatibleProvider
{
    private readonly IConfiguration _configuration;

    public override string ProviderId => _providerConfig?.ProviderId ?? "mistral";

    public override string ProviderName => _providerConfig?.ProviderName ?? "Mistral AI";

    public MistralProvider(HttpClient httpClient, ILogger<MistralProvider> logger, IConfiguration configuration)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override async Task<LLMProviderResponse> ProcessAsync(LLMProviderRequest request)
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
            var mistralRequest = new MistralRequest
            {
                Model = request.ModelId,
                Messages = BuildMessages(request),
                Temperature = request.Temperature
            };

            var endpoint = GetChatCompletionsEndpoint();
            var mistralResponse = await PostJsonAsync<MistralRequest, OpenAIResponse>(endpoint, mistralRequest);

            if (mistralResponse?.Choices == null || !mistralResponse.Choices.Any())
            {
                return CreateErrorResponse($"Invalid response from {ProviderName}");
            }

            var choice = mistralResponse.Choices.First();
            var result = new LLMProviderResponse
            {
                Content = choice.Message?.Content ?? string.Empty,
                Success = true,
                Usage = new LLMUsage
                {
                    InputTokens = mistralResponse.Usage?.PromptTokens ?? 0,
                    OutputTokens = mistralResponse.Usage?.CompletionTokens ?? 0,
                    Cost = CalculateCost(request, 
                        mistralResponse.Usage?.PromptTokens ?? 0, 
                        mistralResponse.Usage?.CompletionTokens ?? 0)
                }
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {Provider} request", ProviderName);
            return CreateErrorResponse("Internal error processing request");
        }
    }
}