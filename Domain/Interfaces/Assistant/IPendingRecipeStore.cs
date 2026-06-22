// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Short-lived store for a recipe paused on an ask step: Save persists the slot bag and resume index
/// for a user/conversation; Peek returns the outstanding recipe (without consuming) so the chat loop
/// can resume it on the next turn; Clear removes it once the recipe completes or is abandoned.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPendingRecipeStore
{
    void Save(PendingRecipe pending);

    PendingRecipe? Peek(Guid userId, string conversationId);

    void Clear(Guid userId, string conversationId);
}
