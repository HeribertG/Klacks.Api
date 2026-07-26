// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Base;

/// <summary>
/// Abstract HTTP base class for LLM providers handling configuration, API key guards, model testing and model discovery.
/// </summary>
public abstract class BaseHttpProvider : ILLMProvider
{
    private const string UnknownModelId = "unknown";

    protected readonly HttpClient _httpClient;
    protected readonly ILogger _logger;
    protected string _apiKey = string.Empty;
    protected bool _requiresApiKey = true;
    protected Domain.Models.Assistant.LLMProvider? _providerConfig;

    public abstract string ProviderId { get; }

    public abstract string ProviderName { get; }

    /// <summary>
    /// Contract family whose built-in parameter rules apply to this provider. The default sends every
    /// parameter, which is what providers without a known restriction did before the rules were
    /// centralised; override only for a family that rejects parameters.
    /// </summary>
    protected virtual LLMParameterDefaultFamily ParameterDefaultFamily => LLMParameterDefaultFamily.Unrestricted;

    /// <summary>
    /// Whether the endpoint accepts "stream_options": {"include_usage": true}. Opt-in per provider:
    /// endpoints that do not know the field reject the entire request with HTTP 400, so an unverified
    /// provider must keep streaming without token counters rather than break every streamed turn.
    /// </summary>
    protected virtual bool SupportsStreamUsage => false;

    /// <summary>
    /// Returns the temperature to send, or null when the parameter must be omitted from the payload
    /// entirely — a literal null value is rejected just like an unsupported value.
    /// </summary>
    /// <param name="request">Carries the target model and any operator-declared parameter overrides</param>
    protected double? ResolveTemperature(LLMProviderRequest request) =>
        LLMModelParameterPolicy.IsSupported(
            ParameterDefaultFamily,
            request.ModelId,
            LLMModelParameterNames.Temperature,
            request.DeclaredParameterSupport)
            ? request.Temperature
            : null;

    /// <summary>
    /// Returns the stream_options block, or null when it must be omitted. The provider opt-in is only
    /// the fallback: an operator declaration on the model wins, so an endpoint that turns out to reject
    /// the field can be corrected in the settings instead of requiring a release.
    /// </summary>
    /// <param name="request">Carries the target model and any operator-declared parameter overrides</param>
    protected OpenAIStreamOptions? ResolveStreamOptions(LLMProviderRequest request) =>
        LLMModelParameterPolicy.IsSupported(
            ParameterDefaultFamily,
            request.ModelId,
            LLMModelParameterNames.StreamOptions,
            request.DeclaredParameterSupport,
            SupportsStreamUsage)
            ? new OpenAIStreamOptions { IncludeUsage = true }
            : null;

    public bool IsEnabled => _providerConfig!.IsEnabled;

    protected BaseHttpProvider(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        this._logger = logger;
    }

    public virtual void Configure(Domain.Models.Assistant.LLMProvider providerConfig)
    {
        _providerConfig = providerConfig;
        _apiKey = providerConfig.ApiKey ?? string.Empty;
        _requiresApiKey = providerConfig.RequiresApiKey;

        if (!string.IsNullOrEmpty(providerConfig.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(providerConfig.BaseUrl);
        }

        ConfigureHttpClient();
    }

    protected bool IsRequiredApiKeyMissing => _requiresApiKey && string.IsNullOrWhiteSpace(_apiKey);

    protected virtual void ConfigureHttpClient()
    {
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    public abstract Task<LLMProviderResponse> ProcessAsync(LLMProviderRequest request, CancellationToken cancellationToken = default);

    public abstract Task<bool> ValidateApiKeyAsync(string apiKey);

    public virtual bool SupportsStreaming => false;

    public virtual IAsyncEnumerable<string> ProcessStreamAsync(
        LLMProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException($"{ProviderName} does not support streaming.");
    }

    protected async IAsyncEnumerable<string> PostStreamAsync<TRequest>(
        string endpoint,
        TRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, GetJsonSerializerOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("{Provider} sending streaming request to {Endpoint}", ProviderName, endpoint);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
        var response = await _httpClient.SendAsync(
            httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("{Provider} streaming API error: {StatusCode} - {Error}",
                ProviderName, response.StatusCode, errorBody);
            throw new InvalidOperationException($"{ProviderName} API error: {response.StatusCode}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var data = line[6..];
            if (data == "[DONE]") yield break;

            yield return data;
        }
    }

    public virtual Task<List<LLMModelDiscovery>?> GetAvailableModelsAsync() =>
        Task.FromResult<List<LLMModelDiscovery>?>(null);

    public virtual async Task<LLMModelTestResult> TestModelAsync(string apiModelId, string? supportedParameters = null)
    {
        if (IsRequiredApiKeyMissing)
            return new LLMModelTestResult(apiModelId, apiModelId, false, "No API key configured", 0);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var request = new LLMProviderRequest
            {
                ModelId = apiModelId,
                Message = "Reply with 'ok'",
                MaxTokens = 5,
                Temperature = 0.0,
                SupportedParameters = supportedParameters,
            };

            using var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var responseTask = ProcessAsync(request, testCts.Token);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15));
            var completed = await Task.WhenAny(responseTask, timeoutTask);
            sw.Stop();

            if (completed == timeoutTask)
                return new LLMModelTestResult(apiModelId, apiModelId, false, "Timeout after 15s", 15000);

            var response = await responseTask;
            return response.Success
                ? new LLMModelTestResult(apiModelId, apiModelId, true, null, (int)sw.ElapsedMilliseconds)
                : new LLMModelTestResult(apiModelId, apiModelId, false, response.Error, (int)sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "TestModelAsync failed for {ModelId}: {Error}", apiModelId, ex.Message);
            return new LLMModelTestResult(apiModelId, apiModelId, false, ex.Message, (int)sw.ElapsedMilliseconds);
        }
    }

    protected async Task<List<LLMModelDiscovery>?> GetModelsFromOpenAIApiAsync()
    {
        if (IsRequiredApiKeyMissing)
            return null;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_httpClient.BaseAddress!, "models"));
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var result = System.Text.Json.JsonSerializer.Deserialize<OpenAIModelsResponse>(
                json, GetJsonSerializerOptions());

            return result?.Data
                .Where(m => !string.IsNullOrWhiteSpace(m.Id))
                .Select(m => new LLMModelDiscovery(m.Id, m.Name ?? m.Id))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch models from {Provider}", ProviderName);
            return null;
        }
    }

    protected async Task<TResponse?> PostJsonAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        string? modelId = null,
        CancellationToken cancellationToken = default)
    {
        var loggedModelId = string.IsNullOrWhiteSpace(modelId) ? UnknownModelId : modelId;

        var json = JsonSerializer.Serialize(request, GetJsonSerializerOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("{Provider} sending request to {Endpoint}: {Request}", ProviderName, endpoint.ForLog(), json.ForLog());

        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogDebug("{Provider} received response: {StatusCode} - {Response}",
            ProviderName, response.StatusCode, responseJson);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = ExtractErrorMessage(responseJson, response.StatusCode);
            var isExpectedModelIncompatibility = LLMProviderErrorMarkers.IsNonChatModelError(responseJson);
            var isTransient = IsTransientStatusCode(response.StatusCode);

            if (isExpectedModelIncompatibility)
            {
                _logger.LogWarning("{Provider} model {Model} is not chat/completions compatible ({StatusCode}): {Error}",
                    ProviderName, loggedModelId, response.StatusCode, errorMessage);
            }
            else if (isTransient)
            {
                _logger.LogWarning("{Provider} transient API error for model {Model} ({StatusCode}): {Error}",
                    ProviderName, loggedModelId, response.StatusCode, errorMessage);
            }
            else
            {
                _logger.LogError("{Provider} API error for model {Model}: {StatusCode} - {Error}",
                    ProviderName, loggedModelId, response.StatusCode, responseJson);
            }

            throw new LLMProviderHttpException(
                $"{ProviderName} API error for model {loggedModelId}: {errorMessage}",
                response.StatusCode,
                isExpectedModelIncompatibility,
                isTransient);
        }

        return JsonSerializer.Deserialize<TResponse>(responseJson, GetJsonSerializerOptions());
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode) =>
        (int)statusCode >= 500 || statusCode == HttpStatusCode.TooManyRequests;

    protected virtual JsonSerializerOptions GetJsonSerializerOptions()
    {
        return new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    protected decimal CalculateCost(LLMProviderRequest request, int inputTokens, int outputTokens)
    {
        return (inputTokens / 1000m * request.CostPerInputToken) + 
               (outputTokens / 1000m * request.CostPerOutputToken);
    }

    /// <summary>
    /// Share of the normal input rate billed for prompt tokens served from the provider's cache.
    /// Only consulted when the model carries no explicit cache-read rate.
    /// </summary>
    protected virtual decimal CacheReadRateMultiplier => 0.5m;

    /// <summary>
    /// Maps OpenAI-shaped token counters onto the internal usage record. PromptTokens includes any
    /// cache hits, so the cached portion is subtracted to keep InputTokens the uncached remainder —
    /// otherwise the same tokens would be counted twice in the total.
    /// </summary>
    protected Domain.Services.Assistant.Providers.LLMUsage BuildUsageFromCounters(LLMProviderRequest request, Shared.OpenAIUsageResponse usage)
    {
        var cacheReadTokens = usage.PromptCacheHitTokens;
        var uncachedInputTokens = Math.Max(0, usage.PromptTokens - cacheReadTokens);
        var cacheReadRate = request.CostPerCacheReadToken
            ?? request.CostPerInputToken * CacheReadRateMultiplier;

        return new Domain.Services.Assistant.Providers.LLMUsage
        {
            InputTokens = uncachedInputTokens,
            OutputTokens = usage.CompletionTokens,
            CacheReadInputTokens = cacheReadTokens,
            Cost = (uncachedInputTokens / 1000m * request.CostPerInputToken)
                   + (cacheReadTokens / 1000m * cacheReadRate)
                   + (usage.CompletionTokens / 1000m * request.CostPerOutputToken)
        };
    }

    protected void LogCacheTelemetry(Shared.OpenAIUsageResponse usage, LLMProviderRequest request)
    {
        var hitRatio = usage.PromptTokens == 0
            ? 0d
            : (double)usage.PromptCacheHitTokens / usage.PromptTokens;

        _logger.LogInformation(
            "{Provider} prompt-cache: model={Model} promptTokens={Prompt} cacheRead={CacheRead} " +
            "uncachedInput={Uncached} output={Output} hitRatio={HitRatio:F2}",
            ProviderName,
            request.ModelId,
            usage.PromptTokens,
            usage.PromptCacheHitTokens,
            usage.PromptTokens - usage.PromptCacheHitTokens,
            usage.CompletionTokens,
            hitRatio);
    }

    protected LLMProviderResponse CreateErrorResponse(string error)
    {
        return new LLMProviderResponse 
        { 
            Success = false, 
            Error = error 
        };
    }

    private string ExtractErrorMessage(string responseJson, HttpStatusCode statusCode)
    {
        try
        {
            var errorObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
            
            if (errorObj.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message))
                {
                    var msg = message.GetString() ?? "";
                    
                    if (msg.Contains("insufficient_quota") || msg.Contains("exceeded your current quota"))
                        return "Insufficient credits. Please add funds to your account.";
                    
                    if (msg.Contains("model") && msg.Contains("not found"))
                        return $"Model not available for your account.";
                    
                    if (msg.Contains("context length") || msg.Contains("maximum context"))
                        return "Message too long. Please reduce the text length.";
                        
                    return msg;
                }
            }
        }
        catch
        {
        }
        
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => "Invalid API key",
            HttpStatusCode.PaymentRequired => "Payment required", 
            HttpStatusCode.TooManyRequests => "Rate limit exceeded",
            HttpStatusCode.NotFound => "Model or endpoint not found",
            _ => $"HTTP {(int)statusCode} error"
        };
    }
}