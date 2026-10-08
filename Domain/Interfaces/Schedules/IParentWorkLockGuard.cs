// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

/// <summary>
/// Guards writes against the lock level of a Work: writes to the Work itself (update, delete) and writes to the
/// entries that hang off it (expenses, WorkChanges). One rule for both: a Closed Work refuses everyone (admins
/// included - the period must be reopened first, because a legacy period close writes no SealedDay rows and the day
/// lock would not stop the write), a Confirmed or Approved Work refuses whoever could not unseal it, and scenario
/// Works (AnalyseToken set) are never checked.
/// </summary>
public interface IParentWorkLockGuard
{
    /// <summary>
    /// Throws InvalidRequestException when the caller may not write a child entry (expense, WorkChange) of the Work.
    /// </summary>
    /// <param name="parentWork">The Work the child entry belongs to</param>
    /// <param name="isAdmin">Whether the caller has the Admin role</param>
    /// <param name="isAuthorised">Whether the caller has the Authorised (supervisor) role</param>
    void EnsureChildWritable(Work parentWork, bool isAdmin, bool isAuthorised);

    /// <summary>
    /// Throws InvalidRequestException when the caller may not update or delete the Work itself.
    /// </summary>
    /// <param name="work">The stored Work about to be updated or deleted</param>
    /// <param name="isAdmin">Whether the caller has the Admin role</param>
    /// <param name="isAuthorised">Whether the caller has the Authorised (supervisor) role</param>
    void EnsureWorkWritable(Work work, bool isAdmin, bool isAuthorised);
}
