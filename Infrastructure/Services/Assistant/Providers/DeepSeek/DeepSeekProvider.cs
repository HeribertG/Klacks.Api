// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// LLM provider for DeepSeek API (OpenAI-compatible tools format).
/// Includes parameter normalization for malformed DeepSeek function call outputs and a
/// one-shot tool_choice fallback: DeepSeek v4 thinking models reject tool_choice=required,
/// so a rejected forcing request is retried once with auto instead of failing the turn.
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
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.DeepSeek;

public class DeepSeekProvider : BaseHttpProvider
{
    private const string ToolChoiceAuto = "auto";
    private const string ToolChoiceUnsupportedErrorMarker = "does not support this tool_choice";
    private const int RawChannelLogLength = 1000;

    public override string ProviderId => _providerConfig!.ProviderId;

    public override string ProviderName => _providerConfig!.ProviderName;

    public override bool SupportsStreaming => true;

    // DeepSeek passes tool_choice through to its OpenAI-style API; thinking models can reject
    // "required", and the one-shot fallback then retries with "auto".
    public bool SupportsToolChoice => true;

    // DeepSeek bills a context-cache hit at roughly a tenth of the miss rate, well below the 0.5
    // default that covers OpenAI-style caching.
    protected override decimal CacheReadRateMultiplier => 0.1m;

    // Verified against the DeepSeek API, which documents stream_options.include_usage.
    protected override bool SupportsStreamUsage => true;

    public DeepSeekProvider(HttpClient httpClient, ILogger<DeepSeekProvider> logger, IConfiguration configuration)
        : base(httpClient, logger)
    {
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
            return await ProcessCoreAsync(request, request.ToolChoice, cancellationToken);
        }
        catch (InvalidOperationException ex) when (IsRequiredToolChoice(request.ToolChoice) && IsToolChoiceRejection(ex))
        {
            _logger.LogWarning(
                "{Provider} rejected tool_choice=required (thinking mode, model {Model}); retrying once with auto",
                ProviderName, request.ModelId);

            try
            {
                return await ProcessCoreAsync(request, ToolChoiceAuto, cancellationToken);
            }
            catch (Exception retryEx)
            {
                _logger.LogError(retryEx, "Error processing {Provider} request after tool_choice fallback", ProviderName);
                return CreateErrorResponse($"{ProviderName}: {retryEx.Message}");
            }
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

    private static string TruncateForLog(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= RawChannelLogLength ? value : value[..RawChannelLogLength] + "...";
    }

    private async Task<LLMProviderResponse> ProcessCoreAsync(
        LLMProviderRequest request, string? toolChoice, CancellationToken cancellationToken)
    {
        var deepSeekRequest = new OpenAIToolsRequest
        {
            Model = request.ModelId,
            Messages = BuildMessages(request),
            Temperature = ResolveTemperature(request),
            MaxTokens = request.MaxTokens,
            Tools = BuildTools(request.AvailableFunctions),
            ToolChoice = request.AvailableFunctions.Any() ? (toolChoice ?? ToolChoiceAuto) : null,
            Stop = LLMStopSequences.Merge(request.StopSequences)
        };

        var endpoint = "chat/completions";
        var deepSeekResponse = await PostJsonAsync<OpenAIToolsRequest, OpenAIResponse>(endpoint, deepSeekRequest, request.ModelId, cancellationToken);

        if (deepSeekResponse?.Choices == null || !deepSeekResponse.Choices.Any())
        {
            return CreateErrorResponse($"Invalid response from {ProviderName}");
        }

        var choice = deepSeekResponse.Choices.First();
        var hasToolCalls = choice.Message?.ToolCalls != null && choice.Message.ToolCalls.Any();
        var rawContent = choice.Message?.GetContentString();
        var rawReasoning = choice.Message?.ReasoningContent;
        _logger.LogInformation(
            "DeepSeek raw channels: hasToolCalls={HasToolCalls}, content ({ContentLength} chars)={Content}, " +
            "reasoning_content ({ReasoningLength} chars)={Reasoning}",
            hasToolCalls, rawContent?.Length ?? 0, TruncateForLog(rawContent),
            rawReasoning?.Length ?? 0, TruncateForLog(rawReasoning));
        var answer = ReasoningContentResolver.Resolve(rawContent, rawReasoning, hasToolCalls);
        var result = new LLMProviderResponse
        {
            Content = answer.Content,
            ContentFromReasoning = answer.FromReasoning,
            Success = true,
            Usage = BuildUsageFromCounters(request, new OpenAIUsageResponse
            {
                PromptTokens = deepSeekResponse.Usage?.PromptTokens ?? 0,
                CompletionTokens = deepSeekResponse.Usage?.CompletionTokens ?? 0,
                PromptCacheHitTokens = deepSeekResponse.Usage?.PromptCacheHitTokens ?? 0,
                PromptCacheMissTokens = deepSeekResponse.Usage?.PromptCacheMissTokens ?? 0
            })
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

    private static bool IsRequiredToolChoice(string? toolChoice) =>
        string.Equals(toolChoice, MutationGuardConstants.ToolChoiceRequired, StringComparison.OrdinalIgnoreCase);

    private static bool IsToolChoiceRejection(Exception ex) =>
        ex.Message.Contains(ToolChoiceUnsupportedErrorMarker, StringComparison.OrdinalIgnoreCase);

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

        var source = StreamCoreAsync(request, request.ToolChoice, cancellationToken).GetAsyncEnumerator(cancellationToken);
        var downgraded = false;
        var anyChunkYielded = false;

        try
        {
            while (true)
            {
                bool moved;
                try
                {
                    moved = await source.MoveNextAsync();
                }
                catch (InvalidOperationException) when (!downgraded && !anyChunkYielded && IsRequiredToolChoice(request.ToolChoice))
                {
                    _logger.LogWarning(
                        "{Provider} stream rejected tool_choice=required (model {Model}); retrying once with auto",
                        ProviderName, request.ModelId);
                    await source.DisposeAsync();
                    source = StreamCoreAsync(request, ToolChoiceAuto, cancellationToken).GetAsyncEnumerator(cancellationToken);
                    downgraded = true;
                    continue;
                }

                if (!moved)
                {
                    break;
                }

                anyChunkYielded = true;
                yield return source.Current;
            }
        }
        finally
        {
            await source.DisposeAsync();
        }
    }

    private async IAsyncEnumerable<string> StreamCoreAsync(
        LLMProviderRequest request,
        string? toolChoice,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var deepSeekRequest = new OpenAIToolsRequest
        {
            Model = request.ModelId,
            Messages = BuildMessages(request),
            Temperature = ResolveTemperature(request),
            MaxTokens = request.MaxTokens,
            Tools = BuildTools(request.AvailableFunctions),
            ToolChoice = request.AvailableFunctions.Any() ? (toolChoice ?? ToolChoiceAuto) : null,
            Stream = true,
            StreamOptions = ResolveStreamOptions(request),
            Stop = LLMStopSequences.Merge(request.StopSequences)
        };

        var endpoint = "chat/completions";

        // Reasoning models (deepseek-reasoner) stream thinking into reasoning_content. It is the ANSWER
        // only when no regular content and no tool call ever arrive; otherwise it is chain-of-thought to
        // discard. Buffer it and flush once after the loop so it never leaks live into the chat.
        var reasoningBuffer = new StringBuilder();
        var sawContent = false;
        var sawToolCall = false;
        OpenAIUsageResponse? streamUsage = null;

        await foreach (var rawJson in PostStreamAsync(endpoint, deepSeekRequest, cancellationToken))
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

            // The usage chunk arrives last and carries an empty choices array, so it must be picked
            // up before the guard below skips it.
            if (chunk?.Usage != null)
            {
                streamUsage = chunk.Usage;
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
                    yield return $"{LLMStreamingTokens.ToolCallPrefix}{tcJson}";
                }
            }

            if (choice.FinishReason == "tool_calls")
            {
                yield return LLMStreamingTokens.ToolCallEnd;
            }
        }

        if (!sawContent && !sawToolCall && reasoningBuffer.Length > 0)
        {
            yield return reasoningBuffer.ToString();
        }

        if (streamUsage != null)
        {
            LogCacheTelemetry(streamUsage, request);
            request.OnStreamUsage?.Invoke(BuildUsageFromCounters(request, streamUsage));
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
