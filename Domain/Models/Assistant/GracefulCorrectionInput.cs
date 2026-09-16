// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Everything the correction planning needs. The previous-action record and the active-recipe flag are
/// passed IN rather than read here: the two chat entry points read both from their stores, while the
/// turn-eval replay builds the record from the goldset item and must never touch either table.
/// </summary>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record GracefulCorrectionInput(
    Agent? Agent,
    IReadOnlyList<string> UserRights,
    string Message,
    string? ConversationId,
    string UserId,
    string? Language,
    AssistantLastAction? LastAction,
    bool RecipeIsActive);
