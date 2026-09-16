// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Everything the turn preparation needs from the chat loop. Provider and model are carried because the
/// recipe resume extracts slots through one structured model call; the conversation id is separate from
/// the context's because the loop resolves it against the persisted conversation before preparing.
/// </summary>

using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record TurnPreparationRequest(
    LLMContext Context,
    ILLMProvider Provider,
    LLMModel Model,
    string ConversationId);
