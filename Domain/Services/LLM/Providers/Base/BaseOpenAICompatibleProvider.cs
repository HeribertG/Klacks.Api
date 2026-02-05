using Klacks.Api.Domain.Services.LLM.Providers.Shared;
using Klacks.Api.Application.DTOs.LLM;
using System.Text.Json;

namespace Klacks.Api.Domain.Services.LLM.Providers.Base;

public abstract class BaseOpenAICompatibleProvider : BaseHttpProvider
{
    protected BaseOpenAICompatibleProvider(HttpClient httpClient, ILogger logger)
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
            var openAIRequest = new OpenAIRequest
            {
                Model = request.ModelId,
                Messages = BuildMessages(request),
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                Functions = MapFunctions(request.AvailableFunctions),
                FunctionCall = request.AvailableFunctions.Any() ? "auto" : null
            };

            var endpoint = GetChatCompletionsEndpoint();
            var openAIResponse = await PostJsonAsync<OpenAIRequest, OpenAIResponse>(endpoint, openAIRequest);

            if (openAIResponse?.Choices == null || !openAIResponse.Choices.Any())
            {
                return CreateErrorResponse($"Invalid response from {ProviderName}");
            }

            var choice = openAIResponse.Choices.First();
            var result = new LLMProviderResponse
            {
                Content = choice.Message?.Content ?? string.Empty,
                Success = true,
                Usage = new LLMUsage
                {
                    InputTokens = openAIResponse.Usage?.PromptTokens ?? 0,
                    OutputTokens = openAIResponse.Usage?.CompletionTokens ?? 0,
                    Cost = CalculateCost(request, 
                        openAIResponse.Usage?.PromptTokens ?? 0, 
                        openAIResponse.Usage?.CompletionTokens ?? 0)
                }
            };

            if (choice.Message?.FunctionCall != null)
            {
                result.FunctionCalls.Add(new LLMFunctionCall
                {
                    FunctionName = choice.Message.FunctionCall.Name,
                    Parameters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                        choice.Message.FunctionCall.Arguments, 
                        GetJsonSerializerOptions()
                    ) ?? new Dictionary<string, object>()
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {Provider} request", ProviderName);
            return CreateErrorResponse("Internal error processing request");
        }
    }

    protected virtual string GetChatCompletionsEndpoint() => "chat/completions";

    protected virtual List<OpenAIMessage> BuildMessages(LLMProviderRequest request)
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

    protected virtual List<OpenAIFunction>? MapFunctions(List<LLMFunction> functions)
    {
        if (!functions.Any())
        {
            return null;
        }

        return functions.Select(f => new OpenAIFunction
        {
            Name = f.Name,
            Description = f.Description,
            Parameters = new OpenAIFunctionParameters
            {
                Type = "object",
                Properties = f.Parameters,
                Required = f.RequiredParameters
            }
        }).ToList();
    }

    public override async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            var testClient = new HttpClient();
            testClient.BaseAddress = _httpClient.BaseAddress;
            testClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            
            var response = await testClient.GetAsync("models");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}