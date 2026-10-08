// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Associations;

/// <summary>
/// The days a client belongs to the company, both bounds inclusive (Membership.ValidFrom / ValidUntil).
/// </summary>
/// <param name="ValidFrom">First member day</param>
/// <param name="ValidUntil">Last member day; null while the membership is open</param>
public sealed record MembershipWindow(DateOnly ValidFrom, DateOnly? ValidUntil)
{
    /// <summary>True when the client is a member on <paramref name="date"/>.</summary>
    public bool Contains(DateOnly date) => date >= ValidFrom && (ValidUntil is null || date <= ValidUntil.Value);

    /// <summary>Number of member days in the inclusive range [<paramref name="from"/>, <paramref name="until"/>].</summary>
    /// <param name="from">First day of the range</param>
    /// <param name="until">Last day of the range</param>
    public int MemberDaysWithin(DateOnly from, DateOnly until)
    {
        var first = from > ValidFrom ? from : ValidFrom;
        var last = ValidUntil is { } end && end < until ? end : until;
        return last < first ? 0 : last.DayNumber - first.DayNumber + 1;
    }
}