// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ILLMProviderFactory
{
    Task<ILLMProvider?> GetProviderAsync(string providerId);
    Task<ILLMProvider?> GetProviderForModelAsync(string modelId);
    Task<List<ILLMProvider>> GetEnabledProvidersAsync();
}
