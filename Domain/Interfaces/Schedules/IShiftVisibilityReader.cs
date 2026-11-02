// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads what the shift write guard needs: which of a set of shift ids exist and are visible for a group
/// restriction, whether one shift exists at all, the stored group assignments of one shift, which of a set of shifts
/// carries an analyse token, the world of the shifts that carry one and the task shifts a container already references.
/// </summary>

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IShiftVisibilityReader
{
    /// <summary>
    /// Returns those of the given shift ids that exist and pass the group-visibility rule for the restriction
    /// (group-less shifts always pass). Missing and hidden ids are both left out, so a caller cannot tell them apart.
    /// </summary>
    /// <param name="shiftIds">Shift ids a write is about to touch</param>
    /// <param name="visibleGroupIds">Flat restriction from IShiftGroupFilterService.ResolveShiftListGroupIdsAsync; never null here</param>
    Task<List<Guid>> FilterVisibleShiftIdsAsync(
        IReadOnlyCollection<Guid> shiftIds, IReadOnlyCollection<Guid> visibleGroupIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// The not deleted group items of one shift as stored, untracked. Empty for a missing shift.
    /// </summary>
    /// <param name="shiftId">Shift whose group assignments are read</param>
    Task<List<GroupItem>> GetActiveGroupItemsAsync(Guid shiftId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when a not deleted shift with the id exists, regardless of its groups.
    /// </summary>
    /// <param name="shiftId">Shift whose existence is checked</param>
    Task<bool> ExistsAsync(Guid shiftId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The first of the given shift ids that belongs to a not deleted shift with an analyse token (variant shift or
    /// scenario clone), or null when none does. Answered with one query whatever the caller's group restriction.
    /// </summary>
    /// <param name="shiftIds">Shift ids a real row is about to point at</param>
    Task<Guid?> FindFirstTokenedShiftIdAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Those of the given shift ids that exist, are not deleted and carry no analyse token, answered with one query.
    /// </summary>
    /// <param name="shiftIds">Shift ids a real row is about to point at</param>
    Task<List<Guid>> FilterRealShiftIdsAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// The world of those given shifts that carry an analyse token, soft-deleted ones included; a real or missing shift has
    /// no entry. Answered with one query whatever the caller's group restriction.
    /// </summary>
    /// <param name="shiftIds">Shift ids Works are about to reference</param>
    Task<IReadOnlyList<ShiftWorld>> GetTokenedShiftWorldsAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// The distinct shift ids the not deleted template items and override items of a container reference as stored.
    /// </summary>
    /// <param name="containerId">Container shift whose templates and overrides are read</param>
    Task<List<Guid>> GetContainerItemShiftIdsAsync(Guid containerId, CancellationToken cancellationToken = default);
}
