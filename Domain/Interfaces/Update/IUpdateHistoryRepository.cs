// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Domain.Interfaces.Update;

/// <summary>
/// Self-committing store of the update_history rows (audit log and hand-off queue of the out-of-process updater): every
/// write calls SaveChanges at once.
/// </summary>
public interface IUpdateHistoryRepository
{
    /// <summary>
    /// Stores a new row. A rejected insert (above all the partial unique index that allows one active row) throws
    /// Klacks.Api.Domain.Exceptions.DatabaseUpdateException, or ConcurrencyException for a concurrency failure.
    /// </summary>
    Task<UpdateHistory> AddAsync(UpdateHistory entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the row read before. Status is a concurrency token: when it moved underneath (the updater claimed or
    /// finished the row), Klacks.Api.Domain.Exceptions.ConcurrencyException is thrown; any other failure propagates
    /// unchanged.
    /// </summary>
    Task UpdateAsync(UpdateHistory entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes the row read before. Status is a concurrency token: when it moved underneath,
    /// Klacks.Api.Domain.Exceptions.ConcurrencyException is thrown; any other failure propagates unchanged.
    /// </summary>
    Task DeleteAsync(UpdateHistory entry, CancellationToken cancellationToken = default);

    Task<UpdateHistory?> GetActiveOperationAsync(CancellationToken cancellationToken = default);

    Task<UpdateHistory?> GetLastSuccessfulUpdateAsync(CancellationToken cancellationToken = default);

    Task<UpdateHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UpdateHistory>> GetRecentAsync(int take, CancellationToken cancellationToken = default);

    Task<UpdateHistory?> GetLatestByTypesAsync(IReadOnlyCollection<UpdateOperationType> types, CancellationToken cancellationToken = default);
}
