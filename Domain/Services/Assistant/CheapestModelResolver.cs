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

        // Ordering by cost only means something when costs are actually maintained. With an unpriced
        // catalog every model ties at zero and the "cheapest" model becomes whichever row the database
        // happened to return first - which silently routed background work to an arbitrary provider.
        // Models without a price are therefore ignored, and if none carries one we fall back to the
        // configured default model, an explicit admin choice, rather than to an accident of ordering.
        var priced = models
            .Where(m => m.CostPerInputToken + m.CostPerOutputToken > 0)
            .ToList();

        var cheapest = priced.Count > 0
            ? priced.OrderBy(m => m.CostPerInputToken + m.CostPerOutputToken).First()
            : models.FirstOrDefault(m => m.IsDefault) ?? models.FirstOrDefault();

        if (cheapest == null)
        {
            return (null, null);
        }

        var provider = await _providerFactory.GetProviderForModelAsync(cheapest.ModelId);
        return (cheapest, provider);
    }
}
