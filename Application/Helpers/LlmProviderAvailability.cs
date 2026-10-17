// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether any configured LLM provider can answer right now: enabled and either holding an API key or not
/// needing one. Shared by the setup-tour and the intro-tour slices of the welcome payload.
/// </summary>
/// <param name="providers">All configured LLM providers</param>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Helpers;

public static class LlmProviderAvailability
{
    public static bool AnyLive(IEnumerable<LLMProvider> providers)
    {
        return providers.Any(p => p.IsEnabled && (p.HasApiKey || !p.RequiresApiKey));
    }
}
