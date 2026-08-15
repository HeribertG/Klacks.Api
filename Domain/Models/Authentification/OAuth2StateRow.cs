// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistent row for a one-time OAuth2 "state" value. Replaces the in-memory state store so the
/// authorization redirect and the callback no longer have to reach the same API instance: with the
/// state in process memory, a callback routed to a second instance finds nothing and the login fails
/// for no reason the user can act on. Deliberately not a <c>BaseEntity</c>: the row is ephemeral and
/// is hard-deleted on consumption or expiry rather than soft-deleted, so a consumed state cannot be
/// replayed from a still-present row.
/// </summary>

namespace Klacks.Api.Domain.Models.Authentification;

public sealed class OAuth2StateRow
{
    public Guid Id { get; set; }

    public string State { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}
