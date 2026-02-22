// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Assistant.Providers;

public interface ILLMProvider
{
    string ProviderId { get; }
    string ProviderName { get; }
    bool IsEnabled { get; }
    
    void Configure(Models.Assistant.LLMProvider providerConfig);
    Task<LLMProviderResponse> ProcessAsync(LLMProviderRequest request);
    Task<bool> ValidateApiKeyAsync(string apiKey);
}