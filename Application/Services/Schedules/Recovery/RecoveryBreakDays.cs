// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.ScheduleRecovery.Model;

namespace Klacks.Api.Application.Services.Schedules.Recovery;

/// <summary>
/// The (client, date) pairs carrying a break, split by meaning: a blocking break (any absence that is not
/// on-call, e.g. sickness or holiday) makes the agent unavailable; an on-call break makes the agent a
/// preferred replacement. A day may appear in both sets; the blocking break then wins. The real time spans of the
/// blocking breaks are kept per (client, date) so a service that only reaches the day through midnight is blocked by the
/// absence it really overlaps; a sealed day has a Blocking entry but no span.
/// </summary>
/// <param name="Blocking">Days with at least one non-on-call break</param>
/// <param name="OnCall">Days with at least one on-call break</param>
public sealed record RecoveryBreakDays(
    HashSet<(Guid ClientId, DateOnly Date)> Blocking,
    HashSet<(Guid ClientId, DateOnly Date)> OnCall)
{
    public Dictionary<CellKey, List<AbsenceWindow>> BlockingWindows { get; } = [];

    public HashSet<(Guid ClientId, DateOnly Date)> Sealed { get; } = [];

    public static RecoveryBreakDays Empty() => new([], []);
}
