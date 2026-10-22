// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces;

/// <summary>
/// Commits the changes staged in the shared scoped context. CompleteAsync and Complete report a rejected write as
/// Klacks.Api.Domain.Exceptions.DatabaseUpdateException (IsDuplicate / IsForeignKeyViolation / ConstraintName from the
/// database) or ConcurrencyException; the final save inside ExecuteInTransactionAsync does not translate (EF Core's
/// exception propagates).
/// </summary>
public interface IUnitOfWork
{
    Task CompleteAsync();

    int Complete();

    Task<ITransaction> BeginTransactionAsync();

    Task CommitTransactionAsync(ITransaction transaction);

    Task RollbackTransactionAsync(ITransaction transaction);

    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation);

    /// <summary>
    /// Stops tracking every entity of the shared scoped context, staged changes included. For a caller that
    /// catches a failed (rolled-back) transaction and keeps working in the same scope: the failed work's
    /// entities otherwise stay tracked as Added/Modified -- or as Unchanged although the rollback discarded
    /// them -- and the next CompleteAsync re-saves them and fails again. Entities read before the call are
    /// detached as well and must be read again before they are changed.
    /// </summary>
    void DiscardTrackedChanges();
}
