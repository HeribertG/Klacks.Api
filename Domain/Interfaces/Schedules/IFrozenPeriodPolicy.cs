// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

/// <summary>
/// Decides which real-plan pay periods are frozen for the period-hours cache. A person's period is frozen when it is
/// sealed (a non-deleted work or break with LockLevel Closed in the period), exported (a non-deleted ExportLogItem of the
/// person overlaps it) and behind the recalculation window (ExportedPeriodWindow). A period reopened after its export is
/// not sealed and therefore not frozen; a scenario (analyse token set) is never frozen.
/// </summary>
public interface IFrozenPeriodPolicy
{
    /// <summary>
    /// Returns the clients of <paramref name="clientIds"/> whose period <paramref name="startDate"/>..<paramref name="endDate"/>
    /// is frozen; empty for a scenario.
    /// </summary>
    Task<IReadOnlySet<Guid>> GetFrozenClientIdsAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken);

    /// <summary>
    /// Returns the cached rows that are not frozen, judging every row by its own client, period and analyse token.
    /// </summary>
    Task<IReadOnlyList<ClientPeriodHours>> ExcludeFrozenRowsAsync(IReadOnlyCollection<ClientPeriodHours> rows);
}
