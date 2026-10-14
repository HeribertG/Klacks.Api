// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What a bulk-delete sentence would remove, resolved on the server from the sentence's parameters: either the exact
/// list of Works, or the reason nothing may be deleted. A plan with a Refusal carries no Works.
/// </summary>
/// <param name="Refusal">Why the call cannot run, or null when the plan can be carried out</param>
/// <param name="EmployeeName">Display name of the employee the Works belong to</param>
/// <param name="From">First day of the range</param>
/// <param name="Until">Last day of the range</param>
/// <param name="Works">The Works the call would delete, oldest first</param>
/// <remarks>ChildWorkIds and ChildBreakIds are the live container children (child Works and Breaks) that the delete takes
/// along with the listed Works; they are read once by the planner and are part of what a confirmation is bound to.</remarks>

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record BulkWorkDeletionPlan(
    string? Refusal,
    string EmployeeName,
    DateOnly From,
    DateOnly Until,
    IReadOnlyList<PlannedWorkDeletion> Works)
{
    public IReadOnlyList<Guid> ChildWorkIds { get; init; } = [];

    public IReadOnlyList<Guid> ChildBreakIds { get; init; } = [];

    public int ChildEntryCount => ChildWorkIds.Count + ChildBreakIds.Count;

    public bool IsRefused => Refusal != null;

    public static BulkWorkDeletionPlan Refuse(string reason) =>
        new(reason, string.Empty, default, default, []);
}
