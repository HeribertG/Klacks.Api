// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistent terminal outcome of a background job (completed, cancelled or failed), kept so a client
/// that missed the SignalR event can still recover it through a status poll. Stored in the database
/// rather than in process memory because the poll may reach a different API instance than the one
/// that ran the job: from there an in-memory cache reports the job as unknown, which a client cannot
/// tell apart from an expired entry, and a failure reason is lost outright. Deliberately not a
/// <c>BaseEntity</c>: the row is ephemeral and is hard-deleted once it expires.
/// </summary>

namespace Klacks.Api.Domain.Models.Schedules;

public sealed class JobTerminalStateRow
{
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ResultJson { get; set; }

    public string? Reason { get; set; }

    public DateTime ExpiresAtUtc { get; set; }
}
