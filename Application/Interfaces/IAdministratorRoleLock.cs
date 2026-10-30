// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Serializes the transactions that remove the Admin role, so that two simultaneous removals cannot both pass the
/// last-administrator count and leave the installation without an active administrator. The lock lives as long as
/// the surrounding database transaction.
/// </summary>
public interface IAdministratorRoleLock
{
    /// <summary>
    /// Waits until no other transaction holds the lock, then holds it until the current transaction ends. Must be
    /// called inside the transaction and before the administrator count is read; outside of a transaction it throws
    /// InvalidOperationException on PostgreSQL.
    /// </summary>
    /// <param name="cancellationToken">Cancels the wait</param>
    Task AcquireAsync(CancellationToken cancellationToken);
}
