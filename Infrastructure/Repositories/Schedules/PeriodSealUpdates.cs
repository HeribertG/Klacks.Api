// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The bulk seal and unseal statements the period close and reopen run on Work and Break. The seal records
/// each entry's lock state (level, SealedAt, SealedBy) in the PreSeal* columns before raising it; the unseal
/// writes that state back and clears the record, so a Confirmed or Approved entry comes back as it was.
/// Entries without a record (sealed before the columns existed) reopen to None, as every unseal did before.
/// All SET expressions of one UPDATE read the row as it was before the statement, so copying a column and
/// overwriting it in the same statement is well defined in PostgreSQL.
/// </summary>
/// <param name="entries">Entries of one period (and optionally one group), already filtered by the caller</param>
/// <param name="level">Seal level; the seal raises entries below it, the unseal reopens entries at it</param>
/// <param name="additionalSetters">Entity-specific columns the same UPDATE sets as well (the sealing group of a Break)</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

internal static class PeriodSealUpdates
{
    public static Task<int> SealAsync<TEntry>(
        IQueryable<TEntry> entries,
        WorkLockLevel level,
        string sealedBy,
        CancellationToken cancellationToken,
        Action<UpdateSettersBuilder<TEntry>>? additionalSetters = null)
        where TEntry : ScheduleEntryBase
    {
        var sealedAt = DateTime.UtcNow;

        return entries
            .Where(e => e.LockLevel < level)
            .ExecuteUpdateAsync(s =>
            {
                s.SetProperty(e => e.PreSealLockLevel, e => (WorkLockLevel?)e.LockLevel)
                    .SetProperty(e => e.PreSealSealedAt, e => e.SealedAt)
                    .SetProperty(e => e.PreSealSealedBy, e => e.SealedBy)
                    .SetProperty(e => e.LockLevel, level)
                    .SetProperty(e => e.SealedAt, sealedAt)
                    .SetProperty(e => e.SealedBy, sealedBy);
                additionalSetters?.Invoke(s);
            }, cancellationToken);
    }

    /// <summary>
    /// Reopens the entries sealed at <paramref name="level"/> and reports what each one went back to. A
    /// recorded level is only written back while it is below the seal level, so an unseal can never leave
    /// an entry at or above the level it was asked to lift.
    /// </summary>
    public static async Task<PeriodUnsealCounts> UnsealAsync<TEntry>(
        IQueryable<TEntry> entries,
        WorkLockLevel level,
        CancellationToken cancellationToken,
        Action<UpdateSettersBuilder<TEntry>>? additionalSetters = null)
        where TEntry : ScheduleEntryBase
    {
        var sealedEntries = entries.Where(e => e.LockLevel == level);

        var countsByPreSealLevel = await sealedEntries
            .GroupBy(e => e.PreSealLockLevel)
            .Select(g => new { Level = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        await sealedEntries.ExecuteUpdateAsync(s =>
        {
            s.SetProperty(
                    e => e.LockLevel,
                    e => e.PreSealLockLevel != null && e.PreSealLockLevel < level ? e.PreSealLockLevel.Value : WorkLockLevel.None)
                .SetProperty(
                    e => e.SealedAt,
                    e => e.PreSealLockLevel != null && e.PreSealLockLevel < level ? e.PreSealSealedAt : null)
                .SetProperty(
                    e => e.SealedBy,
                    e => e.PreSealLockLevel != null && e.PreSealLockLevel < level ? e.PreSealSealedBy : null)
                .SetProperty(e => e.PreSealLockLevel, (WorkLockLevel?)null)
                .SetProperty(e => e.PreSealSealedAt, (DateTime?)null)
                .SetProperty(e => e.PreSealSealedBy, (string?)null);
            additionalSetters?.Invoke(s);
        }, cancellationToken);

        return PeriodUnsealCounts.FromPreSealLevels(countsByPreSealLevel.Select(c => (c.Level, c.Count)));
    }
}
