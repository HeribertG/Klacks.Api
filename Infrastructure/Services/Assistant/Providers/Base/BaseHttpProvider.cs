// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Base;

public abstract class BaseHttpProvider : ILLMProvider
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogger _logger;
    protected string _apiKey = string.Empty;
    protected Domain.Models.Assistant.LLMProvider? _providerConfig;

    public abstract string ProviderId { get; }

    public abstract string ProviderName { get; }

    public bool IsEnabled => _providerConfig!.IsEnabled;

    protected BaseHttpProvider(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        this._logger = logger;
    }

    public virtual void Configure(Domain.Models.Assistant.LLMProvider providerConfig)
    {
        _providerConfig = providerConfig;
        _apiKey = providerConfig.ApiKey!;
        
        if (!string.IsNullOrEmpty(providerConfig.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(providerConfig.BaseUrl);
        }

        ConfigureHttpClient();
    }

    protected virtual void ConfigureHttpClient()
    {
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    public abstract Task<LLMProviderResponse> ProcessAsync(LLMProviderRequest request);

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

    protected async Task<List<LLMModelDiscovery>?> GetModelsFromOpenAIApiAsync()
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

    protected async Task<TResponse?> PostJsonAsync<TRequest, TResponse>(string endpoint, TRequest request)
    {
        var json = JsonSerializer.Serialize(request, GetJsonSerializerOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        _logger.LogDebug("{Provider} sending request to {Endpoint}: {Request}", ProviderName, endpoint, json);

        var response = await _httpClient.PostAsync(endpoint, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        _logger.LogDebug("{Provider} received response: {StatusCode} - {Response}",
            ProviderName, response.StatusCode, responseJson);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("{Provider} API error: {StatusCode} - {Error}", ProviderName, response.StatusCode, responseJson);
            
            var errorMessage = ExtractErrorMessage(responseJson, response.StatusCode);
            throw new InvalidOperationException($"{ProviderName} API error: {errorMessage}");
        }

        return JsonSerializer.Deserialize<TResponse>(responseJson, GetJsonSerializerOptions());
    }

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