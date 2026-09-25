// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What the closing of a streamed turn needs from the tool loop it follows: the provider and model of the
/// turn, the loop's final message and prompts, the running history with its budget, and where in the
/// streamed text the last provider call started.
/// </summary>
/// <param name="Provider">The provider the turn ran on.</param>
/// <param name="Model">The model the turn ran on.</param>
/// <param name="CurrentMessage">The loop's final message, the last tool results.</param>
/// <param name="SystemPrompt">The turn's stable system prompt.</param>
/// <param name="VolatilePrompt">The turn's volatile system prompt.</param>
/// <param name="RunningHistory">The history the loop built up.</param>
/// <param name="HistoryBudget">Token budget the running history is fitted to.</param>
/// <param name="LastCallStart">Offset in the streamed text where the last provider call's content starts.</param>
/// <param name="IsMutationIntent">True when the user's message asks for a change.</param>

using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal sealed record TurnClosingInput(
    ILLMProvider Provider,
    Klacks.Api.Domain.Models.Assistant.LLMModel Model,
    string CurrentMessage,
    string SystemPrompt,
    string? VolatilePrompt,
    List<Providers.LLMMessage> RunningHistory,
    int HistoryBudget,
    int LastCallStart,
    bool IsMutationIntent);
