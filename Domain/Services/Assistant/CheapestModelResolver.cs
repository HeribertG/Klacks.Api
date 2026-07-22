// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the cheapest enabled LLM model together with its configured provider. Shared by every
/// background/sub-loop path that needs a low-cost model (conversation compaction, read-only research)
/// so the "cheapest model" selection lives in one place instead of being duplicated per caller.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public class CheapestModelResolver : ICheapestModelResolver
{
    private readonly ILLMRepository _llmRepository;
    private readonly ILLMProviderFactory _providerFactory;

    public CheapestModelResolver(
        ILLMRepository llmRepository,
        ILLMProviderFactory providerFactory)
    {
        _llmRepository = llmRepository;
        _providerFactory = providerFactory;
    }

    public async Task<(LLMModel? Model, ILLMProvider? Provider)> ResolveAsync(
        CancellationToken cancellationToken = default)
    {
        var models = await _llmRepository.GetModelsAsync(onlyEnabled: true);

        var cheapest = models
            .OrderBy(m => m.CostPerInputToken + m.CostPerOutputToken)
            .FirstOrDefault();

        if (cheapest == null)
        {
            return (null, null);
        }

        var provider = await _providerFactory.GetProviderForModelAsync(cheapest.ModelId);
        return (cheapest, provider);
    }
}
