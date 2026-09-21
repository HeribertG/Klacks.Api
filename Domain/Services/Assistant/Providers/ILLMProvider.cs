// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Assistant.Providers;

public interface ILLMProvider
{
    string ProviderId { get; }
    string ProviderName { get; }
    bool IsEnabled { get; }
    bool SupportsStreaming => false;

    /// <summary>
    /// True when the provider really sends the requested tool_choice value (e.g. "required") instead of
    /// silently using its default. OpenAI-compatible, Gemini and Mistral currently ignore the request
    /// and always send "auto", so they report false. Measured into llm_usage (W1.9).
    /// </summary>
    bool SupportsToolChoice => false;

    /// <summary>
    /// How this provider handles a forced tool call for THIS request. Decided per request, so a provider
    /// whose behaviour depends on the model or on its own configuration answers for the request at hand
    /// instead of for a name on a list. The default is the weakest value: a provider that does not declare
    /// is treated as unable to force, which engages the caller's fallback rather than silently dropping
    /// the tool call. Every provider in this code base declares explicitly; the default exists for test
    /// doubles implementing this interface directly.
    /// </summary>
    /// <param name="request">The request about to be sent, including its ToolChoice and model id</param>
    Domain.Enums.ForcedToolChoiceSupport ResolveForcedToolChoiceSupport(LLMProviderRequest request) =>
        Domain.Enums.ForcedToolChoiceSupport.NotSupported;

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

    /// <summary>
    /// Probes a single model with a minimal completion request.
    /// </summary>
    /// <param name="apiModelId">Provider-side model identifier to test</param>
    /// <param name="supportedParameters">Operator declarations for this model, so the probe sends the
    /// same payload the real turn would; without it a declared override would not show up in the check</param>
    Task<Models.Assistant.LLMModelTestResult> TestModelAsync(string apiModelId, string? supportedParameters = null) =>
        Task.FromResult(new Models.Assistant.LLMModelTestResult(
            apiModelId, apiModelId, false, "Provider does not support testing", 0));
}