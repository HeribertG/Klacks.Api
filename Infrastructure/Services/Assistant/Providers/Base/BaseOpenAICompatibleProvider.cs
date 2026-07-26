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
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Base;

public abstract class BaseOpenAICompatibleProvider : BaseHttpProvider
{
    protected BaseOpenAICompatibleProvider(HttpClient httpClient, ILogger logger)
        : base(httpClient, logger)
    {
    }

    protected override LLMParameterDefaultFamily ParameterDefaultFamily => LLMParameterDefaultFamily.OpenAiCompatible;

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
                Temperature = ResolveTemperature(request),
                MaxTokens = request.MaxTokens,
                Functions = MapFunctions(request.AvailableFunctions),
                FunctionCall = request.AvailableFunctions.Any() ? "auto" : null
            };

            var endpoint = GetChatCompletionsEndpoint();
            var openAIResponse = await PostJsonAsync<OpenAIRequest, OpenAIResponse>(endpoint, openAIRequest, request.ModelId, cancellationToken);

            if (openAIResponse?.Choices == null || !openAIResponse.Choices.Any())
            {
                return CreateErrorResponse($"Invalid response from {ProviderName}");
            }

            var choice = openAIResponse.Choices.First();
            var result = new LLMProviderResponse
            {
                Content = choice.Message?.GetContentString() ?? string.Empty,
                Success = true,
                Usage = BuildUsageFromCounters(request, new OpenAIUsageResponse
                {
                    PromptTokens = openAIResponse.Usage?.PromptTokens ?? 0,
                    CompletionTokens = openAIResponse.Usage?.CompletionTokens ?? 0,
                    PromptCacheHitTokens = openAIResponse.Usage?.PromptCacheHitTokens ?? 0,
                    PromptCacheMissTokens = openAIResponse.Usage?.PromptCacheMissTokens ?? 0
                })
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
            Temperature = ResolveTemperature(request),
            MaxTokens = request.MaxTokens,
            Functions = MapFunctions(request.AvailableFunctions),
            FunctionCall = request.AvailableFunctions.Any() ? "auto" : null,
            Stream = true,
            StreamOptions = ResolveStreamOptions(request)
        };

        var endpoint = GetChatCompletionsEndpoint();
        var jsonOptions = GetJsonSerializerOptions();
        OpenAIUsageResponse? streamUsage = null;

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

            // The usage chunk arrives last and carries an empty choices array, so it must be picked up
            // before the guard below skips it.
            if (chunk?.Usage != null)
            {
                streamUsage = chunk.Usage;
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

        if (streamUsage != null)
        {
            LogCacheTelemetry(streamUsage, request);
            request.OnStreamUsage?.Invoke(BuildUsageFromCounters(request, streamUsage));
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