// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistent row for a planning-profile draft being collected across dialog turns, so a setup started
/// with start_planning_profile_setup survives a backend restart instead of being silently dropped before
/// the admin finishes set_planning_profile_parameters/apply. Deliberately not a <c>BaseEntity</c>: the
/// row is ephemeral and is hard-deleted on Clear/expiry rather than soft-deleted.
/// </summary>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed class PendingPlanningProfileDraftRow
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string ConversationId { get; set; } = string.Empty;

    public string DraftJson { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}
