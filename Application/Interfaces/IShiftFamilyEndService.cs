// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Ends a whole real shift family (sealed order, plannable copy and cut pieces, or one row that stands alone without an
/// order) on one day. Used by the edit form and by the set_sealed_order_until_date skill.
/// </summary>
public interface IShiftFamilyEndService
{
    /// <summary>
    /// Checks the end date, writes the family and commits, in one transaction. Runs its own transaction, so it must not
    /// be called inside IUnitOfWork.ExecuteInTransactionAsync. Throws ShiftEndDateRefusedException before anything is
    /// written. Clears the change tracker first, so the caller must not stage anything before. Refuses when the caller
    /// does not see every row of the family (reason hiddenRows). Does not check rights, and the caller still answers a
    /// hidden addressed row itself, like a missing one.
    /// </summary>
    /// <param name="entry">The stored row the caller was addressed with: the sealed order, the plannable copy, a cut piece or a row without an order</param>
    /// <param name="untilDate">The inclusive last day of the family</param>
    /// <param name="saveEntry">Saves the other content of the addressed row inside the transaction; null when there is none. Runs again on a database retry</param>
    /// <param name="cancellationToken">Cancels the reads</param>
    Task EndFamilyAsync(Shift entry, DateOnly untilDate, Func<Task>? saveEntry, CancellationToken cancellationToken);
}
