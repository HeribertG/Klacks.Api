using Klacks.Api.Domain.Services.LLM.Providers.Base;
using Microsoft.Extensions.Configuration;

namespace Klacks.Api.Domain.Services.LLM.Providers.Azure;

public class AzureOpenAIProvider : BaseOpenAICompatibleProvider
{
    private readonly IConfiguration _configuration;
    private string _deploymentName = string.Empty;
    private string _apiVersion = "2024-02-15-preview";

    public override string ProviderId => "azure";
    public override string ProviderName => "Azure OpenAI";

    public AzureOpenAIProvider(HttpClient httpClient, ILogger<AzureOpenAIProvider> logger, IConfiguration configuration)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override void Configure(Models.LLM.LLMProvider providerConfig)
    {
        base.Configure(providerConfig);
        
        if (providerConfig.Settings != null)
        {
            if (providerConfig.Settings.TryGetValue("deploymentName", out var deployment))
            {
                _deploymentName = deployment.ToString() ?? string.Empty;
            }
            
            if (providerConfig.Settings.TryGetValue("apiVersion", out var version))
            {
                _apiVersion = version.ToString() ?? _apiVersion;
            }
        }
    }

    protected override void ConfigureHttpClient()
    {
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
        }
    }

    protected override string GetChatCompletionsEndpoint()
    {
        return $"openai/deployments/{_deploymentName}/chat/completions?api-version={_apiVersion}";
    }

    public override async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            var testClient = new HttpClient();
            testClient.BaseAddress = _httpClient.BaseAddress;
            testClient.DefaultRequestHeaders.Add("api-key", apiKey);
            
            var response = await testClient.GetAsync($"openai/deployments?api-version={_apiVersion}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}