// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Puts a cut family back to the content of its sealed order. Called by IShiftCutFacade after it has answered visibility,
/// container and variant rules.
/// </summary>
public interface IShiftFamilyReset
{
    /// <summary>
    /// Checks, writes and commits in one transaction. Throws ShiftResetRefusedException before anything is written. Clears
    /// the change tracker first, so the caller must not stage anything before, and must not call it inside
    /// IUnitOfWork.ExecuteInTransactionAsync.
    /// </summary>
    /// <param name="order">The sealed order, visible to the caller, with its group items</param>
    /// <param name="requestedStart">The day that was asked for; it counts only when the family has real work</param>
    /// <returns>The rows below the order after the reset, empty when the order has none</returns>
    Task<List<Shift>> ResetAsync(Shift order, DateOnly requestedStart);
}
