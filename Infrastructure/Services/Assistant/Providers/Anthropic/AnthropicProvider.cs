// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// LLM provider for the Anthropic Messages API with SSE streaming and prompt-caching support.
/// Streaming yields text deltas immediately; tool-call JSON is accumulated per block index and
/// emitted as a NUL-prefixed TOOL token so the shared function-execution layer can reconstruct
/// function calls. Prompt-caching is enabled via the anthropic-beta header; the system prompt
/// is sent as a one-element content-block array with ephemeral cache_control so Anthropic can
/// reuse the KV cache across turns.
/// </summary>

using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using LLMFunction = Klacks.Api.Domain.Models.Assistant.LLMFunction;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic;

public class AnthropicProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AnthropicProvider> _logger;
    private readonly IConfiguration _configuration;

    private const string AnthropicVersionHeader = "anthropic-version";
    private const string AnthropicBetaHeader = "anthropic-beta";
    private const string PromptCachingBeta = "prompt-caching-2024-07-31";

    private const string SseEventPrefix = "event: ";
    private const string SseDataPrefix = "data: ";
    private const string SseEventContentBlockDelta = "content_block_delta";
    private const string SseEventContentBlockStart = "content_block_start";
    private const string SseEventMessageStop = "message_stop";
    private const string DeltaTypeTextDelta = "text_delta";
    private const string DeltaTypeInputJsonDelta = "input_json_delta";
    private const string ContentBlockTypeToolUse = "tool_use";

    private string _apiKey = string.Empty;
    private Domain.Models.Assistant.LLMProvider? _providerConfig;

    public string ProviderId => _providerConfig!.ProviderId;

    public string ProviderName => _providerConfig!.ProviderName;

    public bool IsEnabled => _providerConfig!.IsEnabled;

    public bool SupportsStreaming => true;

    public AnthropicProvider(HttpClient httpClient, ILogger<AnthropicProvider> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public void Configure(Domain.Models.Assistant.LLMProvider providerConfig)
    {
        _providerConfig = providerConfig;
        _apiKey = providerConfig.ApiKey!;

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Remove("x-api-key");
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Remove(AnthropicVersionHeader);
            _httpClient.DefaultRequestHeaders.Add(AnthropicVersionHeader, providerConfig.ApiVersion!);
            _httpClient.DefaultRequestHeaders.Remove(AnthropicBetaHeader);
            _httpClient.DefaultRequestHeaders.Add(AnthropicBetaHeader, PromptCachingBeta);
        }

        _httpClient.BaseAddress = new Uri(providerConfig.BaseUrl!);
    }

    public async Task<LLMProviderResponse> ProcessAsync(LLMProviderRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return new LLMProviderResponse
            {
                Success = false,
                Error = "Anthropic provider is not enabled"
            };
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            return new LLMProviderResponse
            {
                Success = false,
                Error = "The provider for the selected model is not available."
            };
        }

        try
        {
            var anthropicRequest = new AnthropicRequest
            {
                Model = request.ModelId,
                Messages = BuildMessages(request),
                System = BuildSystemContent(request.SystemPrompt),
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                Tools = MapTools(request.AvailableFunctions)
            };

            var options = BuildSerializerOptions();
            var json = JsonSerializer.Serialize(anthropicRequest, options);
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("messages", requestContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                var errorMessage = ExtractAnthropicErrorMessage(error, response.StatusCode);
                _logger.LogError("Anthropic API error: {StatusCode} - {Error}", response.StatusCode, error);
                return new LLMProviderResponse
                {
                    Success = false,
                    Error = errorMessage
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var anthropicResponse = JsonSerializer.Deserialize<AnthropicResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (anthropicResponse == null)
            {
                return new LLMProviderResponse
                {
                    Success = false,
                    Error = "Invalid response from Anthropic"
                };
            }

            var result = new LLMProviderResponse
            {
                Content = ExtractContent(anthropicResponse),
                Success = true,
                Usage = new LLMUsage
                {
                    InputTokens = anthropicResponse.Usage?.InputTokens ?? 0,
                    OutputTokens = anthropicResponse.Usage?.OutputTokens ?? 0,
                    Cost = CalculateCost(request, anthropicResponse.Usage)
                }
            };

            if (anthropicResponse.Content != null)
            {
                foreach (var content in anthropicResponse.Content)
                {
                    if (content.Type == ContentBlockTypeToolUse && content.Name != null)
                    {
                        result.FunctionCalls.Add(new LLMFunctionCall
                        {
                            FunctionName = content.Name,
                            Parameters = content.Input ?? new Dictionary<string, object>()
                        });
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Anthropic request");
            return new LLMProviderResponse
            {
                Success = false,
                Error = "Internal error processing request"
            };
        }
    }

    public async IAsyncEnumerable<string> ProcessStreamAsync(
        LLMProviderRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            throw new InvalidOperationException("Anthropic provider is not enabled");

        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("The provider for the selected model is not available.");

        var anthropicRequest = new AnthropicRequest
        {
            Model = request.ModelId,
            Messages = BuildMessages(request),
            System = BuildSystemContent(request.SystemPrompt),
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            Tools = MapTools(request.AvailableFunctions),
            Stream = true
        };

        var options = BuildSerializerOptions();
        var json = JsonSerializer.Serialize(anthropicRequest, options);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "messages")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(
            httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var errorMessage = ExtractAnthropicErrorMessage(errorBody, response.StatusCode);
            _logger.LogError("Anthropic streaming API error: {StatusCode} - {Error}", response.StatusCode, errorBody);
            throw new InvalidOperationException(errorMessage);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var toolJsonAccumulators = new Dictionary<int, (string Name, StringBuilder Json)>();
        string? currentEventType = null;
        bool hasToolCalls = false;

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;

            if (line.StartsWith(SseEventPrefix, StringComparison.Ordinal))
            {
                currentEventType = line[SseEventPrefix.Length..];
                continue;
            }

            if (!line.StartsWith(SseDataPrefix, StringComparison.Ordinal))
                continue;

            var data = line[SseDataPrefix.Length..];
            if (string.IsNullOrWhiteSpace(data)) continue;

            if (currentEventType == SseEventMessageStop)
            {
                if (hasToolCalls)
                    yield return " TOOL_END";
                yield break;
            }

            AnthropicStreamEvent? evt;
            try
            {
                evt = JsonSerializer.Deserialize<AnthropicStreamEvent>(data, options);
            }
            catch
            {
                continue;
            }

            if (evt == null) continue;

            if (currentEventType == SseEventContentBlockStart
                && evt.ContentBlock?.Type == ContentBlockTypeToolUse
                && evt.ContentBlock.Name != null)
            {
                toolJsonAccumulators[evt.Index] = (evt.ContentBlock.Name, new StringBuilder());
                hasToolCalls = true;
            }
            else if (currentEventType == SseEventContentBlockDelta && evt.Delta != null)
            {
                if (evt.Delta.Type == DeltaTypeTextDelta && evt.Delta.Text != null)
                {
                    yield return evt.Delta.Text;
                }
                else if (evt.Delta.Type == DeltaTypeInputJsonDelta && evt.Delta.PartialJson != null)
                {
                    if (toolJsonAccumulators.TryGetValue(evt.Index, out var acc))
                    {
                        acc.Json.Append(evt.Delta.PartialJson);
                        toolJsonAccumulators[evt.Index] = acc;
                    }
                }
            }
        }

        foreach (var (index, (name, jsonBuilder)) in toolJsonAccumulators)
        {
            var tcJson = JsonSerializer.Serialize(new
            {
                toolCall = true,
                index,
                name,
                arguments = jsonBuilder.ToString()
            }, options);
            yield return $" TOOL:{tcJson}";
        }

        if (hasToolCalls)
            yield return " TOOL_END";
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            var testRequest = new
            {
                model = "claude-3-haiku-20240307",
                messages = new[] { new { role = "user", content = "Hi" } },
                max_tokens = 10
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
            {
                Content = new StringContent(JsonSerializer.Serialize(testRequest), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add(AnthropicVersionHeader, "2024-01-01");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.PaymentRequired;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<Domain.Models.Assistant.LLMModelDiscovery>?> GetAvailableModelsAsync()
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
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var result = JsonSerializer.Deserialize<AnthropicModelsResponse>(json, options);

            return result?.Data
                .Where(m => !string.IsNullOrWhiteSpace(m.Id))
                .Select(m => new Domain.Models.Assistant.LLMModelDiscovery(m.Id, m.DisplayName ?? m.Id))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch models from Anthropic");
            return null;
        }
    }

    private List<AnthropicMessage> BuildMessages(LLMProviderRequest request)
    {
        var messages = new List<AnthropicMessage>();

        foreach (var msg in request.ConversationHistory)
        {
            if (msg.Role != "system" && !string.IsNullOrWhiteSpace(msg.Content))
            {
                messages.Add(new AnthropicMessage { Role = msg.Role, Content = msg.Content });
            }
        }

        if (request.ImagePng is { Length: > 0 })
        {
            messages.Add(new AnthropicMessage
            {
                Role = "user",
                Content = new object[]
                {
                    new AnthropicImageBlock
                    {
                        Source = new AnthropicImageSource
                        {
                            Data = Convert.ToBase64String(request.ImagePng),
                        },
                    },
                    new AnthropicTextBlock
                    {
                        Text = string.IsNullOrWhiteSpace(request.Message) ? " " : request.Message,
                    },
                },
            });
        }
        else if (!string.IsNullOrWhiteSpace(request.Message))
        {
            messages.Add(new AnthropicMessage { Role = "user", Content = request.Message });
        }

        return messages;
    }

    private static object? BuildSystemContent(string? systemPrompt)
    {
        if (string.IsNullOrEmpty(systemPrompt))
            return null;

        return new[]
        {
            new AnthropicSystemBlock
            {
                Type = "text",
                Text = systemPrompt,
                CacheControl = new AnthropicCacheControl { Type = "ephemeral" }
            }
        };
    }

    private static List<object>? MapTools(List<LLMFunction> functions)
    {
        if (functions.Count == 0)
            return null;

        return functions.Select(f => (object)new AnthropicTool
        {
            Name = f.Name,
            Description = f.Description,
            InputSchema = new AnthropicInputSchema
            {
                Type = "object",
                Properties = f.Parameters,
                Required = f.RequiredParameters
            }
        }).ToList();
    }

    private static string ExtractContent(AnthropicResponse response)
    {
        if (response.Content == null || !response.Content.Any())
            return string.Empty;

        var textContents = response.Content
            .Where(c => c.Type == "text")
            .Select(c => c.Text ?? string.Empty);

        return string.Join("\n", textContents);
    }

    private static decimal CalculateCost(LLMProviderRequest request, AnthropicUsage? usage)
    {
        if (usage == null) return 0;

        return (usage.InputTokens / 1000m * request.CostPerInputToken) +
               (usage.OutputTokens / 1000m * request.CostPerOutputToken);
    }

    private static JsonSerializerOptions BuildSerializerOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private string ExtractAnthropicErrorMessage(string responseJson, System.Net.HttpStatusCode statusCode)
    {
        try
        {
            var errorObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (errorObj.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message))
                {
                    var msg = message.GetString() ?? "";

                    if (msg.Contains("api key"))
                        return "Invalid Anthropic API key";

                    if (msg.Contains("max_tokens"))
                        return "max_tokens field required";

                    if (msg.Contains("anthropic-version"))
                        return "Invalid Anthropic API version";

                    return msg;
                }
            }
        }
        catch
        {
        }

        return statusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "Invalid Anthropic API key",
            System.Net.HttpStatusCode.PaymentRequired => "Anthropic payment required",
            System.Net.HttpStatusCode.TooManyRequests => "Anthropic rate limit exceeded",
            System.Net.HttpStatusCode.NotFound => "Anthropic model not found",
            _ => $"Anthropic API error: {(int)statusCode}"
        };
    }
}
