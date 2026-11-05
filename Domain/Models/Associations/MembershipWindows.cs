// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Membership-day rule over a set of loaded membership windows: a client is a member on a date when the date lies
/// inside the client's window (both bounds inclusive) or when the client has no window (no membership row, unrestricted).
/// </summary>

namespace Klacks.Api.Domain.Models.Associations;

public static class MembershipWindows
{
    /// <summary>True when <paramref name="clientId"/> is a member on <paramref name="date"/>.</summary>
    /// <param name="windows">Membership window per client; a missing entry means unrestricted.</param>
    /// <param name="clientId">Client to check.</param>
    /// <param name="date">Day to check.</param>
    public static bool IsMemberOn(IReadOnlyDictionary<Guid, MembershipWindow> windows, Guid clientId, DateOnly date) =>
        !windows.TryGetValue(clientId, out var window) || window.Contains(date);
}
