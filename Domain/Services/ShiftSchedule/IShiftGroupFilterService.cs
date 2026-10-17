// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.ShiftSchedule;

public interface IShiftGroupFilterService
{
    /// <summary>
    /// Group ids for the is_group_restricted flags of the schedule entries and for the planning-period check.
    /// A selected group is returned only when the caller may see it; a hidden selection is ignored and falls back
    /// to the caller's visible roots, exactly as if no group had been selected. An empty list means "no
    /// restriction" to the existing callers (admin), so a hidden selection must never collapse to an empty list.
    /// </summary>
    /// <param name="selectedGroupId">Group chosen in the UI filter or the X-Selected-Group header; null when none</param>
    Task<List<Guid>> GetVisibleGroupIdsAsync(Guid? selectedGroupId);

    /// <summary>
    /// Group filter for the shift schedule stored procedures. Null means unrestricted (admin, installation
    /// without groups, or no caller at all, i.e. a background job). A list - possibly empty - restricts the
    /// result to shifts of those groups and their subgroups; an empty list therefore means "no group is
    /// visible" and never "everything". A selected group the caller may not see yields an empty list.
    /// </summary>
    /// <param name="selectedGroupId">Group chosen in the UI filter; null when no group is selected</param>
    Task<List<Guid>?> ResolveShiftScheduleGroupIdsAsync(Guid? selectedGroupId);

    /// <summary>
    /// Group filter for EF queries over shifts (shift/order list, by-id reads). Null means unrestricted. A list -
    /// possibly empty - is the flat set of group ids a shift must belong to (subgroups already expanded, since an
    /// EF Contains does not recurse); shifts without any group stay visible to everyone. A selected group is
    /// matched exactly and yields an empty list when the caller may not see it.
    /// </summary>
    /// <param name="selectedGroupId">Group chosen in the UI filter; null when no group is selected</param>
    Task<List<Guid>?> ResolveShiftListGroupIdsAsync(Guid? selectedGroupId);
}
