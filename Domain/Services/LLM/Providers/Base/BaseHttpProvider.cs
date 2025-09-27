using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Klacks.Api.Domain.Services.LLM.Providers.Base;

public abstract class BaseHttpProvider : ILLMProvider
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogger _logger;
    protected string _apiKey = string.Empty;
    protected Models.LLM.LLMProvider? _providerConfig;

    public abstract string ProviderId { get; }

    public abstract string ProviderName { get; }

    public bool IsEnabled => _providerConfig?.IsEnabled ?? false;

    protected BaseHttpProvider(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public virtual void Configure(Models.LLM.LLMProvider providerConfig)
    {
        _providerConfig = providerConfig;
        _apiKey = providerConfig.ApiKey ?? string.Empty;
        
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
            return default;
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
}