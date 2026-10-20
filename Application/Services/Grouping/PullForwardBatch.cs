// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Groups;

namespace Klacks.Api.Application.Services.Grouping;

/// <summary>
/// The existing memberships a run staged for a pull-forward to the employment entry date, and the groups whose start was
/// lowered to their members' earliest entry. Kept so the same run can verify the committed change.
/// </summary>
/// <param name="ItemStarts">New start per membership (GroupItem) id</param>
/// <param name="GroupStarts">Lowest new member start per group id (never after today)</param>
/// <param name="ChangedCount">Memberships the repository actually changed (an item that already started on or before its new date is not counted)</param>
public sealed record PullForwardBatch(
    IReadOnlyDictionary<Guid, DateTime> ItemStarts,
    IReadOnlyDictionary<Guid, DateTime> GroupStarts,
    int ChangedCount = 0)
{
    public static PullForwardBatch Empty { get; } = new(new Dictionary<Guid, DateTime>(), new Dictionary<Guid, DateTime>());

    public int Count => ItemStarts.Count;

    public GroupMembershipStartInfo ReportOn(GroupMembershipStartInfo info) =>
        info with { ExistingMembersPulledForwardCount = ChangedCount };
}
