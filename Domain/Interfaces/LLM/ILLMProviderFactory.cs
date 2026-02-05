using Klacks.Api.Domain.Services.LLM.Providers;

namespace Klacks.Api.Domain.Interfaces.LLM;

public interface ILLMProviderFactory
{
    Task<ILLMProvider?> GetProviderAsync(string providerId);
    Task<ILLMProvider?> GetProviderForModelAsync(string modelId);
    Task<List<ILLMProvider>> GetEnabledProvidersAsync();
}
