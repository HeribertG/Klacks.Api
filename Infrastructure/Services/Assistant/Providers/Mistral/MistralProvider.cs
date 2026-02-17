using Klacks.Api.Infrastructure.Services.Assistant.Providers.Base;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Mistral;

public class MistralProvider : BaseHttpProvider
{
    public override string ProviderId => _providerConfig!.ProviderId;

    public override string ProviderName => _providerConfig!.ProviderName;

    public MistralProvider(HttpClient httpClient, ILogger<MistralProvider> logger, IConfiguration configuration)
        : base(httpClient, logger)
    {
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
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                Tools = BuildTools(request.AvailableFunctions),
                ToolChoice = request.AvailableFunctions.Any() ? "auto" : null
            };

            var endpoint = "chat/completions";
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

            if (choice.Message?.ToolCalls != null && choice.Message.ToolCalls.Any())
            {
                foreach (var toolCall in choice.Message.ToolCalls)
                {
                    if (toolCall.Function != null)
                    {
                        result.FunctionCalls.Add(new LLMFunctionCall
                        {
                            FunctionName = toolCall.Function.Name,
                            Parameters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                                toolCall.Function.Arguments,
                                GetJsonSerializerOptions()
                            ) ?? new Dictionary<string, object>()
                        });
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {Provider} request", ProviderName);
            return CreateErrorResponse("Internal error processing request");
        }
    }

    public override async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            var testClient = new HttpClient();
            testClient.BaseAddress = _httpClient.BaseAddress;
            testClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await testClient.GetAsync("models");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private List<OpenAIMessage> BuildMessages(LLMProviderRequest request)
    {
        var messages = new List<OpenAIMessage>();

        if (!string.IsNullOrEmpty(request.SystemPrompt))
        {
            messages.Add(new OpenAIMessage { Role = "system", Content = request.SystemPrompt });
        }

        foreach (var msg in request.ConversationHistory)
        {
            messages.Add(new OpenAIMessage { Role = msg.Role, Content = msg.Content });
        }

        messages.Add(new OpenAIMessage { Role = "user", Content = request.Message });

        return messages;
    }

    private List<MistralTool>? BuildTools(List<LLMFunction> functions)
    {
        if (!functions.Any())
        {
            return null;
        }

        return functions.Select(f => new MistralTool
        {
            Type = "function",
            Function = new MistralFunction
            {
                Name = f.Name,
                Description = f.Description,
                Parameters = new MistralFunctionParameters
                {
                    Type = "object",
                    Properties = f.Parameters,
                    Required = f.RequiredParameters.Any() ? f.RequiredParameters : null
                }
            }
        }).ToList();
    }
}
