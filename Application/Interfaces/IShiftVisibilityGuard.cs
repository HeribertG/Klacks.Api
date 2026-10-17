// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Single place that decides whether the calling user may write a shift, using the same rule as the shift reads
/// (IShiftGroupFilterService.ResolveShiftListGroupIdsAsync + ShiftGroupVisibilityFilter): admins and background
/// callers without a user are unrestricted, everybody else may touch group-less shifts and shifts with at least one
/// visible group. A hidden shift must be answered exactly like a missing one by each caller.
/// </summary>
public interface IShiftVisibilityGuard
{
    /// <summary>
    /// True for an unrestricted caller without any query (the caller's own existence check decides as before);
    /// for a restricted caller true only when the shift exists and is visible, so missing and hidden look alike.
    /// </summary>
    /// <param name="shiftId">Shift a write is about to touch</param>
    Task<bool> IsVisibleAsync(Guid shiftId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Same rule for a shift that is already loaded with its group items, answered without a shift query.
    /// </summary>
    /// <param name="shift">Shift loaded including GroupItems</param>
    Task<bool> IsVisibleAsync(Shift shift, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the first of the given shift ids that a restricted caller may not touch (missing or hidden), or null
    /// when all may be touched. Answered with one query; an unrestricted caller gets null without a query.
    /// </summary>
    /// <param name="shiftIds">Shift ids a write is about to touch</param>
    Task<Guid?> FindFirstHiddenAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refuses a write that adds, removes or re-dates a group assignment of a group the caller cannot see, with the
    /// same KeyNotFoundException text as a missing group. Unchanged assignments are not checked.
    /// </summary>
    /// <param name="stored">Group items of the shift as stored (or of the shift a new piece is cut from); empty for a new shift</param>
    /// <param name="requested">Group items the write will save</param>
    Task EnsureGroupChangesVisibleAsync(
        IReadOnlyCollection<GroupItem> stored, IReadOnlyCollection<GroupItem> requested, CancellationToken cancellationToken = default);

    /// <summary>
    /// Like the overload with stored items, reading the stored group items of the shift first.
    /// </summary>
    /// <param name="shiftId">Stored shift whose assignments are compared</param>
    /// <param name="requested">Group items the write will save</param>
    Task EnsureGroupChangesVisibleAsync(
        Guid shiftId, IReadOnlyCollection<GroupItem> requested, CancellationToken cancellationToken = default);
}
