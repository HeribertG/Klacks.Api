// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads what the shift write guard needs: which of a set of shift ids exist and are visible for a group
/// restriction, and the stored group assignments of one shift.
/// </summary>

using Klacks.Api.Domain.Models.Associations;

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
}
