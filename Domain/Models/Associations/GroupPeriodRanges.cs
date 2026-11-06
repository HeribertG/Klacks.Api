// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Group-period rule over a set of loaded group periods (GroupItemValidityRange per client): a client may be planned
/// for the group on a date when the date lies inside the client's period (both bounds inclusive, a missing bound leaves
/// that side open) or when the client has no period (no item in the group, unrestricted). The planning period guard and
/// the wizards' closed days use this one rule.
/// </summary>

namespace Klacks.Api.Domain.Models.Associations;

public static class GroupPeriodRanges
{
    /// <summary>True when <paramref name="clientId"/> lies inside its group period on <paramref name="date"/>.</summary>
    /// <param name="ranges">Group period per client; a missing entry means unrestricted.</param>
    /// <param name="clientId">Client to check.</param>
    /// <param name="date">Day to check.</param>
    public static bool IsInsideOn(
        IReadOnlyDictionary<Guid, (DateTime? ValidFrom, DateTime? ValidUntil)> ranges,
        Guid clientId,
        DateOnly date) =>
        !ranges.TryGetValue(clientId, out var range) || Covers(range, date);

    /// <summary>True when <paramref name="date"/> lies inside <paramref name="range"/>.</summary>
    /// <param name="range">The group period; a null bound leaves that side open.</param>
    /// <param name="date">Day to check.</param>
    public static bool Covers((DateTime? ValidFrom, DateTime? ValidUntil) range, DateOnly date) =>
        (range.ValidFrom is not { } from || DateOnly.FromDateTime(from) <= date)
        && (range.ValidUntil is not { } until || DateOnly.FromDateTime(until) >= date);
}
