// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Groups;

/// <summary>
/// Combines the validity bounds of an employee's group items into one group period: the earliest start and the latest
/// end, where an item without a start (or without an end) leaves that side open. LINQ Min/Max skip null and would let a
/// bounded item narrow an open-ended one, locking days the employee is in fact assigned to.
/// </summary>
/// <param name="items">The ValidFrom/ValidUntil pairs of the employee's group items; at least one</param>
public static class GroupItemValidityRange
{
    public static (DateTime? ValidFrom, DateTime? ValidUntil) Combine(
        IReadOnlyCollection<(DateTime? ValidFrom, DateTime? ValidUntil)> items)
    {
        var validFrom = items.Any(item => item.ValidFrom == null) ? null : items.Min(item => item.ValidFrom);
        var validUntil = items.Any(item => item.ValidUntil == null) ? null : items.Max(item => item.ValidUntil);
        return (validFrom, validUntil);
    }
}
