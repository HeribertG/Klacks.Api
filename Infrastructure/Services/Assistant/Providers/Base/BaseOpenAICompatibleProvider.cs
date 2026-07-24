// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Base class for OpenAI-compatible LLM providers using the legacy function_call API format.
/// </summary>

using System.Runtime.CompilerServices;
using System.Text.Json;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;
using LLMFunction = Klacks.Api.Domain.Models.Assistant.LLMFunction;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Base;

public abstract class BaseOpenAICompatibleProvider : BaseHttpProvider
{
    // OpenAI's reasoning-oriented models reject a caller-supplied temperature: gpt-5-nano fails with
    // "temperature does not support 0" and gpt-5-search-api with "incompatible request argument:
    // temperature". Omitting the field is always accepted (the API then applies its own default of 1),
    // so this is an allow-list of the families that DO honour a custom temperature; every other model,
    // including unknown or future ones, has it omitted, which is the fail-safe default. Matched by prefix
    // because of dated variants such as gpt-4o-2024-08-06. The "chat" marker covers the non-reasoning
    // chat variants (e.g. gpt-5-chat-latest) that still accept temperature.
    private const string ChatModelMarker = "chat";

    private static readonly string[] TemperatureCapableModelPrefixes =
    [
        "gpt-3.5",
        "gpt-4",
        "gpt-5.2",
        "gpt-5.3",
        "gpt-5.4"
    ];

    protected BaseOpenAICompatibleProvider(HttpClient httpClient, ILogger logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Returns the temperature to send for a model, or null when the model rejects the parameter.
    /// </summary>
    /// <param name="apiModelId">Provider-side model identifier the request targets</param>
    /// <param name="requestedTemperature">Temperature the caller asked for</param>
    private static double? ResolveTemperature(string apiModelId, double requestedTemperature)
    {
        var supportsTemperature =
            apiModelId.Contains(ChatModelMarker, StringComparison.OrdinalIgnoreCase) ||
            TemperatureCapableModelPrefixes.Any(prefix =>
                apiModelId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        return supportsTemperature ? requestedTemperature : null;
    }

    public override async Task<LLMProviderResponse> ProcessAsync(LLMProviderRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return CreateErrorResponse($"{ProviderName} provider is not enabled");
        }

        if (IsRequiredApiKeyMissing)
        {
            return CreateErrorResponse("The provider for the selected model is not available.");
        }

        try
        {
            var openAIRequest = new OpenAIRequest
            {
                Model = request.ModelId,
                Messages = BuildMessages(request),
                Temperature = ResolveTemperature(request.ModelId, request.Temperature),
                MaxTokens = request.MaxTokens,
                Functions = MapFunctions(request.AvailableFunctions),
                FunctionCall = request.AvailableFunctions.Any() ? "auto" : null
            };

            var endpoint = GetChatCompletionsEndpoint();
            var openAIResponse = await PostJsonAsync<OpenAIRequest, OpenAIResponse>(endpoint, openAIRequest, cancellationToken);

            if (openAIResponse?.Choices == null || !openAIResponse.Choices.Any())
            {
                return CreateErrorResponse($"Invalid response from {ProviderName}");
            }

            var choice = openAIResponse.Choices.First();
            var result = new LLMProviderResponse
            {
                Content = choice.Message?.GetContentString() ?? string.Empty,
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
        catch (LLMProviderHttpException ex) when (ex.IsExpected)
        {
            _logger.LogDebug(
                "{Provider} chat request skipped ({Reason}): {Message}",
                ProviderName,
                ex.IsTransient ? "transient upstream error" : "model not chat-completions compatible",
                ex.Message);
            return CreateErrorResponse($"{ProviderName}: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {Provider} request", ProviderName);
            return CreateErrorResponse($"{ProviderName}: {ex.Message}");
        }
    }

    public override bool SupportsStreaming => true;

    public override async IAsyncEnumerable<string> ProcessStreamAsync(
        LLMProviderRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException($"{ProviderName} provider is not enabled");
        }

        if (IsRequiredApiKeyMissing)
        {
            throw new InvalidOperationException("The provider for the selected model is not available.");
        }

        var openAIRequest = new OpenAIRequest
        {
            Model = request.ModelId,
            Messages = BuildMessages(request),
            Temperature = ResolveTemperature(request.ModelId, request.Temperature),
            MaxTokens = request.MaxTokens,
            Functions = MapFunctions(request.AvailableFunctions),
            FunctionCall = request.AvailableFunctions.Any() ? "auto" : null,
            Stream = true
        };

        var endpoint = GetChatCompletionsEndpoint();
        var jsonOptions = GetJsonSerializerOptions();

        await foreach (var data in PostStreamAsync(endpoint, openAIRequest, cancellationToken))
        {
            OpenAIStreamChunk? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<OpenAIStreamChunk>(data, jsonOptions);
            }
            catch (JsonException)
            {
                continue;
            }

            if (chunk?.Choices == null || chunk.Choices.Count == 0)
            {
                continue;
            }

            var choice = chunk.Choices[0];

            if (choice.Delta?.Content != null)
            {
                yield return choice.Delta.Content;
            }

            if (choice.Delta?.FunctionCall != null)
            {
                var toolPayload = JsonSerializer.Serialize(new
                {
                    toolCall = true,
                    index = 0,
                    name = choice.Delta.FunctionCall.Name ?? string.Empty,
                    arguments = choice.Delta.FunctionCall.Arguments ?? string.Empty
                }, jsonOptions);

                yield return $"{LLMStreamingTokens.ToolCallPrefix}{toolPayload}";
            }

            if (choice.FinishReason == "function_call")
            {
                yield return LLMStreamingTokens.ToolCallEnd;
            }
        }
    }

    protected virtual string GetChatCompletionsEndpoint() => "chat/completions";

    protected virtual List<OpenAIMessage> BuildMessages(LLMProviderRequest request)
    {
        var messages = new List<OpenAIMessage>();

        if (!string.IsNullOrEmpty(request.EffectiveSystemPrompt))
        {
            messages.Add(new OpenAIMessage { Role = "system", Content = request.EffectiveSystemPrompt });
        }

        foreach (var msg in request.ConversationHistory)
        {
            messages.Add(new OpenAIMessage { Role = msg.Role, Content = msg.Content });
        }

        if (request.ImagePng is { Length: > 0 })
        {
            var dataUri = "data:image/png;base64," + Convert.ToBase64String(request.ImagePng);
            messages.Add(new OpenAIMessage
            {
                Role = "user",
                Content = new object[]
                {
                    new OpenAITextContent(request.Message),
                    new OpenAIImageContent(new OpenAIImageUrl(dataUri)),
                }
            });
        }
        else
        {
            messages.Add(new OpenAIMessage { Role = "user", Content = request.Message });
        }

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
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_httpClient.BaseAddress!, "models"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}