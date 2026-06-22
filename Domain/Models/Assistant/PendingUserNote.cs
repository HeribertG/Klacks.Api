// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class PendingUserNote : BaseEntity
{
    public Guid AgentId { get; set; }

    /// <summary>
    /// The user this note is intended for. Null means a broadcast addressed to every user;
    /// a value scopes the note to that single user. A personal note is soft-deleted the moment
    /// it is delivered; a broadcast stays active for its lifetime so it reaches every user, then
    /// the broadcast cleanup background service soft-deletes it.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// The information the agent stashed to relay to the user later.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional short subject grouping related notes (e.g. "shift", "reminder").
    /// </summary>
    public string? Topic { get; set; }

    /// <summary>
    /// For a broadcast note (UserId == null): the moment it was first relayed to any user.
    /// The broadcast cleanup background service soft-deletes the note one lifetime after this
    /// timestamp, so other users still receive it in the meantime. Null until first delivered.
    /// Unused for personal notes, which are soft-deleted immediately on delivery.
    /// </summary>
    public DateTime? FirstDeliveredAt { get; set; }
}
