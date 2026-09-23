// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Adds the usage of one provider call to a turn's running usage record. Shared by the chat loops and the
/// closing guard (EmptyAnswerRecovery), so every provider call of a turn lands in the same totals without
/// the guard having to call back into the chat service.
/// </summary>
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class LLMUsageAccumulator
{
    /// <summary>
    /// Adds every counter of the call's usage to the turn's totals.
    /// </summary>
    /// <param name="total">The turn's running usage record, mutated in place.</param>
    /// <param name="current">The usage reported for one provider call.</param>
    internal static void Add(LLMUsage total, LLMUsage current)
    {
        total.InputTokens += current.InputTokens;
        total.OutputTokens += current.OutputTokens;
        total.CacheCreationInputTokens += current.CacheCreationInputTokens;
        total.CacheReadInputTokens += current.CacheReadInputTokens;
        total.Cost += current.Cost;
    }
}
