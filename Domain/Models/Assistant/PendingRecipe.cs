// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A recipe paused mid-flow waiting for the user's next message. Holds the recipe name, the index of
/// the step to resume at (always an ask step), and the slot bag accumulated so far. Scoped to a user
/// and conversation and expires after a short TTL so an abandoned recipe never resurrects.
/// </summary>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed class PendingRecipe
{
    public Guid UserId { get; set; }

    public string ConversationId { get; set; } = string.Empty;

    public string RecipeName { get; set; } = string.Empty;

    public int StepIndex { get; set; }

    public Dictionary<string, string> Slots { get; set; } = new();

    public DateTime ExpiresAtUtc { get; set; }
}
