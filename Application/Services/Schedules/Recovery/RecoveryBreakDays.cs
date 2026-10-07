// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules.Recovery;

/// <summary>
/// The (client, date) pairs carrying a break, split by meaning: a blocking break (any absence that is not
/// on-call, e.g. sickness or holiday) makes the agent unavailable; an on-call break makes the agent a
/// preferred replacement. A day may appear in both sets; the blocking break then wins.
/// </summary>
/// <param name="Blocking">Days with at least one non-on-call break</param>
/// <param name="OnCall">Days with at least one on-call break</param>
public sealed record RecoveryBreakDays(
    HashSet<(Guid ClientId, DateOnly Date)> Blocking,
    HashSet<(Guid ClientId, DateOnly Date)> OnCall)
{
    public static RecoveryBreakDays Empty() => new([], []);
}
