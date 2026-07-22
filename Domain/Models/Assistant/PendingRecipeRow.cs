// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistent row for a recipe paused mid-flow. Replaces the in-memory pending-recipe store so a
/// paused guided flow (its slot bag, resume index and confirmation state) survives a backend restart
/// instead of being silently dropped between the ask turn and the user's answer. Deliberately not a
/// <c>BaseEntity</c>: the row is ephemeral and is hard-deleted on Clear/expiry rather than soft-deleted.
/// </summary>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed class PendingRecipeRow
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string ConversationId { get; set; } = string.Empty;

    public string RecipeName { get; set; } = string.Empty;

    public int StepIndex { get; set; }

    public string SlotsJson { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public bool AwaitingConfirmation { get; set; }

    public bool CaptureRewindUsed { get; set; }
}
