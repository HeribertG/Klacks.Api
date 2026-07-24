// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Derives the per-turn <see cref="Klacks.Api.Domain.Models.Assistant.ContextBudgetProfile"/> from a
/// provider/model pair. Implementations MUST derive every cap exclusively from
/// <see cref="Klacks.Api.Domain.Services.Assistant.Providers.ILLMProvider.GetEffectiveInputTokenLimit"/>
/// and never from <see cref="Klacks.Api.Domain.Models.Assistant.LLMModel.ContextWindow"/> directly —
/// the nominal context window can overstate what the provider actually accepts in a single request
/// (documented production incident 2026-07-07).
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IContextBudgetPolicy
{
    ContextBudgetProfile Resolve(ILLMProvider provider, LLMModel model);
}
