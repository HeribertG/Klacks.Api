namespace Klacks.Api.Domain.Services.LLM.Providers;

public interface ILLMProvider
{
    string ProviderId { get; }
    string ProviderName { get; }
    bool IsEnabled { get; }
    
    void Configure(Models.LLM.LLMProvider providerConfig);
    Task<LLMProviderResponse> ProcessAsync(LLMProviderRequest request);
    Task<bool> ValidateApiKeyAsync(string apiKey);
}