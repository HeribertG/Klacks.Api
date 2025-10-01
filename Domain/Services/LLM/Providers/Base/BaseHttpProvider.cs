using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Net;
using Klacks.Api.Presentation.DTOs.LLM;

namespace Klacks.Api.Domain.Services.LLM.Providers.Base;

public abstract class BaseHttpProvider : ILLMProvider
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogger _logger;
    protected string _apiKey = string.Empty;
    protected Models.LLM.LLMProvider? _providerConfig;

    public abstract string ProviderId { get; }

    public abstract string ProviderName { get; }

    public bool IsEnabled => _providerConfig!.IsEnabled;

    protected BaseHttpProvider(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        this._logger = logger;
    }

    public virtual void Configure(Models.LLM.LLMProvider providerConfig)
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
            // Fallback wenn JSON parsing fehlschlägt
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