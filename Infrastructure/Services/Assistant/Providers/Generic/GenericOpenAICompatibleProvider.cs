// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Generic LLM provider for any OpenAI-compatible API (tools format).
/// Supports custom base URLs, includes parameter normalization for malformed outputs.
/// </summary>

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Base;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;
using LLMFunction = Klacks.Api.Domain.Models.Assistant.LLMFunction;
using LLMModelDiscovery = Klacks.Api.Domain.Models.Assistant.LLMModelDiscovery;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Generic;

public class GenericOpenAICompatibleProvider : BaseHttpProvider
{
    public override string ProviderId => _providerConfig!.ProviderId;

    public override string ProviderName => _providerConfig!.ProviderName;

    public override bool SupportsStreaming => true;

    private const string CodingAgentUserAgent = "claude-code/1.0";

    // Some Kimi (Moonshot AI) models reject any temperature other than exactly 1 ("invalid temperature:
    // only 1 is allowed for this model" for kimi-for-coding-highspeed and k3). The error guarantees 1 is
    // valid, so those models get a forced temperature of 1; omitting instead would rely on an unverified
    // server default. The rule is gated on the Kimi endpoint so the other OpenAI-compatible backends that
    // also use this provider (Groq, OpenRouter, Cerebras, local Ollama, ...) are untouched, and it targets
    // only the failing model prefixes so kimi-for-coding keeps its caller-supplied temperature.
    private const string KimiEndpointMarker = "kimi";
    private const double KimiRequiredTemperature = 1.0;

    private static readonly string[] KimiFixedTemperatureModelPrefixes =
    [
        "kimi-for-coding-highspeed",
        "k3"
    ];

    public GenericOpenAICompatibleProvider(HttpClient httpClient, ILogger<GenericOpenAICompatibleProvider> logger, IConfiguration configuration)
        : base(httpClient, logger)
    {
    }

    private bool IsKimiEndpoint =>
        _providerConfig?.BaseUrl?.Contains(KimiEndpointMarker, StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Returns the temperature to send for a model, forcing the Kimi-required value where the model
    /// rejects any other temperature, and passing the caller's value through everywhere else.
    /// </summary>
    /// <param name="apiModelId">Provider-side model identifier the request targets</param>
    /// <param name="requestedTemperature">Temperature the caller asked for</param>
    private double ResolveTemperature(string apiModelId, double requestedTemperature)
    {
        if (IsKimiEndpoint && KimiFixedTemperatureModelPrefixes.Any(prefix =>
                apiModelId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return KimiRequiredTemperature;
        }

        return requestedTemperature;
    }

    protected override void ConfigureHttpClient()
    {
        base.ConfigureHttpClient();

        if (_providerConfig?.BaseUrl?.Contains("api.kimi.com") == true)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(CodingAgentUserAgent);
        }
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
            var openAiRequest = new OpenAIToolsRequest
            {
                Model = request.ModelId,
                Messages = BuildMessages(request),
                Temperature = ResolveTemperature(request.ModelId, request.Temperature),
                MaxTokens = request.MaxTokens,
                Tools = BuildTools(request.AvailableFunctions),
                ToolChoice = request.AvailableFunctions.Any() ? (request.ToolChoice ?? "auto") : null,
                Stop = LLMStopSequences.Merge(request.StopSequences)
            };

            var endpoint = "chat/completions";
            var response = await PostJsonAsync<OpenAIToolsRequest, OpenAIResponse>(endpoint, openAiRequest, request.ModelId, cancellationToken);

            if (response?.Choices == null || !response.Choices.Any())
            {
                return CreateErrorResponse($"Invalid response from {ProviderName}");
            }

            var choice = response.Choices.First();
            var hasToolCalls = choice.Message?.ToolCalls != null && choice.Message.ToolCalls.Any();
            var result = new LLMProviderResponse
            {
                Content = ReasoningContentResolver.EffectiveContent(
                    choice.Message?.GetContentString(), choice.Message?.ReasoningContent, hasToolCalls),
                Success = true,
                Usage = new LLMUsage
                {
                    InputTokens = response.Usage?.PromptTokens ?? 0,
                    OutputTokens = response.Usage?.CompletionTokens ?? 0,
                    Cost = CalculateCost(request,
                        response.Usage?.PromptTokens ?? 0,
                        response.Usage?.CompletionTokens ?? 0)
                }
            };

            if (hasToolCalls)
            {
                foreach (var toolCall in choice.Message!.ToolCalls!)
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

                NormalizeMalformedParameters(result.FunctionCalls, request.AvailableFunctions);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {Provider} request: {Message}", ProviderName, ex.Message);
            return CreateErrorResponse($"Internal error processing request: {ex.Message}");
        }
    }

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

        var openAiRequest = new OpenAIToolsRequest
        {
            Model = request.ModelId,
            Messages = BuildMessages(request),
            Temperature = ResolveTemperature(request.ModelId, request.Temperature),
            MaxTokens = request.MaxTokens,
            Tools = BuildTools(request.AvailableFunctions),
            ToolChoice = request.AvailableFunctions.Any() ? (request.ToolChoice ?? "auto") : null,
            Stream = true,
            Stop = LLMStopSequences.Merge(request.StopSequences)
        };

        var endpoint = "chat/completions";

        // Reasoning models stream their thinking into reasoning_content. It is the ANSWER only when no
        // regular content and no tool call ever arrive (e.g. Kimi reasoning-only); otherwise it is
        // chain-of-thought to discard. Buffer it and flush once after the loop so it never leaks live.
        var reasoningBuffer = new StringBuilder();
        var sawContent = false;
        var sawToolCall = false;

        await foreach (var rawJson in PostStreamAsync(endpoint, openAiRequest, cancellationToken))
        {
            OpenAIStreamChunk? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<OpenAIStreamChunk>(rawJson, GetJsonSerializerOptions());
            }
            catch
            {
                continue;
            }

            if (chunk?.Choices == null || chunk.Choices.Count == 0)
            {
                continue;
            }

            var choice = chunk.Choices[0];
            var delta = choice.Delta;

            if (!string.IsNullOrEmpty(delta?.Content))
            {
                sawContent = true;
                yield return delta!.Content!;
            }

            if (!string.IsNullOrEmpty(delta?.ReasoningContent))
            {
                reasoningBuffer.Append(delta!.ReasoningContent);
            }

            if (delta?.ToolCalls != null)
            {
                sawToolCall = true;
                foreach (var tc in delta.ToolCalls)
                {
                    var tcJson = JsonSerializer.Serialize(new
                    {
                        toolCall = true,
                        index = tc.Index,
                        name = tc.Function?.Name,
                        arguments = tc.Function?.Arguments
                    }, GetJsonSerializerOptions());
                    yield return $" TOOL:{tcJson}";
                }
            }

            if (choice.FinishReason == "tool_calls")
            {
                yield return " TOOL_END";
            }
        }

        if (!sawContent && !sawToolCall && reasoningBuffer.Length > 0)
        {
            yield return reasoningBuffer.ToString();
        }
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

    public override Task<List<LLMModelDiscovery>?> GetAvailableModelsAsync() => GetModelsFromOpenAIApiAsync();

    private List<OpenAIMessage> BuildMessages(LLMProviderRequest request)
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

        messages.Add(new OpenAIMessage { Role = "user", Content = request.Message });

        return messages;
    }

    private void NormalizeMalformedParameters(List<LLMFunctionCall> calls, List<LLMFunction> functions)
    {
        foreach (var call in calls)
        {
            var funcDef = functions.FirstOrDefault(f => f.Name == call.FunctionName);
            if (funcDef == null || funcDef.Parameters.Count == 0) continue;

            var expectedNames = funcDef.Parameters.Keys.ToList();

            if (call.Parameters.Keys.Any(k => expectedNames.Contains(k, StringComparer.OrdinalIgnoreCase)))
                continue;

            var rawValue = string.Join(", ", call.Parameters.Values.Select(v => v?.ToString() ?? ""));
            if (string.IsNullOrWhiteSpace(rawValue)) continue;

            var normalized = ExtractParametersFromString(rawValue, expectedNames);
            if (normalized.Count > 0)
            {
                _logger.LogInformation(
                    "Normalized malformed parameters for {Function}: {ParamCount} params extracted from raw string",
                    call.FunctionName, normalized.Count);
                call.Parameters = normalized;
            }
        }
    }

    private static readonly Dictionary<string, string[]> ParameterAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["firstName"] = ["Vorname", "First Name"],
        ["lastName"] = ["Nachname", "Last Name"],
        ["email"] = ["E-Mail", "Email-Adresse", "Mail"],
        ["userId"] = ["User-ID", "Benutzer-ID", "BenutzerID"],
        ["groupNames"] = ["Gruppenname", "Gruppennamen", "Group Name", "Group Names"],
        ["name"] = ["Name", "Bezeichnung"],
        ["script"] = ["Script", "Skript", "Code"],
    };

    private static Dictionary<string, object> ExtractParametersFromString(string raw, List<string> expectedNames)
    {
        var result = new Dictionary<string, object>();

        foreach (var paramName in expectedNames)
        {
            var patterns = new List<string> { Regex.Escape(paramName) };
            if (ParameterAliases.TryGetValue(paramName, out var aliases))
            {
                patterns.AddRange(aliases.Select(Regex.Escape));
            }

            var pattern = $@"(?:{string.Join("|", patterns)})\s*[:=]\s*(?:'([^']+)'|""([^""]+)""|(\S[^,\n]*))";
            var match = Regex.Match(raw, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var value = match.Groups[1].Success ? match.Groups[1].Value :
                            match.Groups[2].Success ? match.Groups[2].Value :
                            match.Groups[3].Value;
                result[paramName] = value.Trim();
            }
        }

        return result;
    }

    private List<OpenAITool>? BuildTools(List<LLMFunction> functions)
    {
        if (!functions.Any())
        {
            return null;
        }

        return functions.Select(f => new OpenAITool
        {
            Type = "function",
            Function = new OpenAIToolFunction
            {
                Name = f.Name,
                Description = f.Description,
                Parameters = new OpenAIToolFunctionParameters
                {
                    Type = "object",
                    Properties = f.Parameters,
                    Required = f.RequiredParameters.Any() ? f.RequiredParameters : null
                }
            }
        }).ToList();
    }
}
