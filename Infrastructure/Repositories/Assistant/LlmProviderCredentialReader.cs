// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads a configured LLM provider's API key from the llm_providers table via ILLMRepository.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class LlmProviderCredentialReader : ILlmProviderCredentialReader
{
    private readonly ILLMRepository _llmRepository;

    public LlmProviderCredentialReader(ILLMRepository llmRepository)
    {
        _llmRepository = llmRepository;
    }

    public async Task<string?> GetApiKeyAsync(string providerId, CancellationToken cancellationToken = default)
    {
        var provider = await _llmRepository.GetProviderByIdAsync(providerId);
        return provider is { IsEnabled: true, HasApiKey: true } ? provider.ApiKey : null;
    }
}
