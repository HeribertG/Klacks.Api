// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What a chat turn starts from once its context is prepared: the model and provider it runs on, its
/// conversation, the stable and volatile system-prompt segments, the history truncated into the turn's
/// budget and the budget profile. When preparation failed only Error is set.
/// </summary>
/// <param name="Model">The model the turn runs on; null when preparation failed.</param>
/// <param name="Provider">The provider serving the model; null when preparation failed.</param>
/// <param name="Error">The user-facing reason preparation failed; null on success.</param>
/// <param name="Conversation">The loaded or newly created conversation; null when preparation failed.</param>
/// <param name="SystemPrompt">The stable (cacheable) system-prompt segment.</param>
/// <param name="VolatilePrompt">The volatile (per-turn) system-prompt segment.</param>
/// <param name="History">The conversation history truncated into the turn's history budget.</param>
/// <param name="BudgetProfile">Per-turn budget caps derived from the model's effective input limit.</param>

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant.ChatTurn;

internal sealed record PreparedChatTurn(
    LLMModel? Model,
    ILLMProvider? Provider,
    string? Error,
    LLMConversation? Conversation,
    string? SystemPrompt,
    string? VolatilePrompt,
    List<Providers.LLMMessage>? History,
    ContextBudgetProfile? BudgetProfile)
{
    internal static PreparedChatTurn Failed(string error) => new(null, null, error, null, null, null, null, null);
}
