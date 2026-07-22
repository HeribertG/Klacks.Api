// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistent row recording one entity the assistant created or updated within a conversation, so a
/// later implicit reference ("it", "the last one") survives a chat-history compaction. Deliberately not
/// a <c>BaseEntity</c>: the row is ephemeral ring state, hard-deleted when it ages past the per-conversation
/// bound rather than soft-deleted (which would let dead rows accumulate forever).
/// </summary>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed class RecentEntityRow
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string ConversationId { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string? DisplayName { get; set; }

    public string Action { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
