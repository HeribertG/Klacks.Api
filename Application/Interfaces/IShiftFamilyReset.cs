// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Puts a cut family back to the content of its sealed origin row from a start day on. Called by IShiftCutFacade after it has answered visibility,
/// container and variant rules.
/// </summary>
public interface IShiftFamilyReset
{
    /// <summary>
    /// Checks, writes and commits in one transaction. Throws ShiftResetRefusedException before anything is written. Clears
    /// the change tracker first, so the caller must not stage anything before, and must not call it inside
    /// IUnitOfWork.ExecuteInTransactionAsync.
    /// </summary>
    /// <param name="order">The sealed origin row, visible to the caller, with its group items</param>
    /// <param name="requestedStart">The day that was asked for; it counts with or without real work, a day before the origin row is moved to its first day</param>
    /// <returns>The rows below the origin row after the reset, empty when it has none</returns>
    Task<List<Shift>> ResetAsync(Shift order, DateOnly requestedStart);
}
