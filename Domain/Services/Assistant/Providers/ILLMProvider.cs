// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Assistant.Providers;

public interface ILLMProvider
{
    string ProviderId { get; }
    string ProviderName { get; }
    bool IsEnabled { get; }
    bool SupportsStreaming => false;

    void Configure(Models.Assistant.LLMProvider providerConfig);
    Task<LLMProviderResponse> ProcessAsync(LLMProviderRequest request, CancellationToken cancellationToken = default);
    Task<bool> ValidateApiKeyAsync(string apiKey);

    /// <summary>
    /// The real maximum number of input (prompt) tokens the provider accepts for this model in a single
    /// request. Defaults to the model's nominal context window. Providers whose nominal window is only
    /// reachable under a special request configuration (e.g. Anthropic's 1M context beta header) override
    /// this to report the actually enforced limit, so history truncation adapts per model automatically.
    /// </summary>
    int GetEffectiveInputTokenLimit(Models.Assistant.LLMModel model) =>
        model.ContextWindow > 0 ? model.ContextWindow : 128_000;

    IAsyncEnumerable<string> ProcessStreamAsync(
        LLMProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException($"{ProviderName} does not support streaming.");
    }

    Task<List<Models.Assistant.LLMModelDiscovery>?> GetAvailableModelsAsync() =>
        Task.FromResult<List<Models.Assistant.LLMModelDiscovery>?>(null);

    Task<Models.Assistant.LLMModelTestResult> TestModelAsync(string apiModelId) =>
        Task.FromResult(new Models.Assistant.LLMModelTestResult(
            apiModelId, apiModelId, false, "Provider does not support testing", 0));
}