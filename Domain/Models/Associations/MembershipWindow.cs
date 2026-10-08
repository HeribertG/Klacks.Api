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
}