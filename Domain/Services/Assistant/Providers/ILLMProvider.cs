// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Assistant.Providers;

public interface ILLMProvider
{
    string ProviderId { get; }
    string ProviderName { get; }
    bool IsEnabled { get; }
    bool SupportsStreaming => false;

    void Configure(Models.Assistant.LLMProvider providerConfig);
    Task<LLMProviderResponse> ProcessAsync(LLMProviderRequest request);
    Task<bool> ValidateApiKeyAsync(string apiKey);

    IAsyncEnumerable<string> ProcessStreamAsync(
        LLMProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException($"{ProviderName} does not support streaming.");
    }

    Task<List<Models.Assistant.LLMModelDiscovery>?> GetAvailableModelsAsync() =>
        Task.FromResult<List<Models.Assistant.LLMModelDiscovery>?>(null);
}