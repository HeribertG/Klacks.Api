// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Token figures the Playground budget books: the up-front estimate of a provider request (prompt size from
/// the same characters-per-token ratio and tool-definition estimate the history budget uses, plus the
/// requested maximum output, capped at MaxReservationTokens) and the weighted real usage a provider reports afterwards.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Playground;

public static class PlaygroundTokenEstimator
{
    /// <param name="request">The request about to be sent to the provider</param>
    public static long EstimateReservation(LLMProviderRequest request)
    {
        long promptChars = request.EffectiveSystemPrompt.Length + request.Message.Length;
        foreach (var message in request.ConversationHistory)
        {
            promptChars += message.Content.Length;
        }

        var promptTokens = promptChars / ConversationHistoryBudget.CharsPerToken;
        var toolTokens = ConversationHistoryBudget.EstimateToolDefinitionReserveTokens(request.AvailableFunctions);

        var estimate = promptTokens + toolTokens + Math.Max(request.MaxTokens, 0);

        return Math.Min(estimate, PlaygroundTokenLimitConstants.MaxReservationTokens);
    }

    /// <summary>
    /// Tokens the provider reported for one call, with cache reads weighted by CacheReadTokenWeightPercent.
    /// Zero means the provider reported nothing, which callers treat as "unknown" and keep the estimate.
    /// </summary>
    /// <param name="usage">The usage the provider reported</param>
    public static long Weigh(LLMUsage usage)
    {
        var cacheReads = (long)usage.CacheReadInputTokens * PlaygroundTokenLimitConstants.CacheReadTokenWeightPercent
            / PlaygroundTokenLimitConstants.PercentBase;

        return (long)usage.InputTokens + usage.CacheCreationInputTokens + usage.OutputTokens + cacheReads;
    }
}
