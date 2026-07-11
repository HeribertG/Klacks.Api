// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// Diagnostic snapshot of the groups visible to the caller: which semantic hypotheses their
/// names support, how many active members each group has, and how many clients of the
/// requested entity type still have no active group membership.
/// </summary>
/// <param name="TotalGroupCount">Total number of non-deleted, named groups in scope.</param>
/// <param name="CategorySummaries">Per-category match counts and example group names, richest first.</param>
/// <param name="GroupMemberCounts">Active client member count per group, richest first.</param>
/// <param name="UngroupedEntityType">The entity type the ungrouped count was computed for.</param>
/// <param name="UngroupedCount">Number of clients of that type without an active group membership.</param>
/// <param name="TotalClientCountForType">Total number of non-deleted clients of that type.</param>
public record AnalyzeGroupSemanticsResult(
    int TotalGroupCount,
    IReadOnlyList<GroupSemanticCategorySummary> CategorySummaries,
    IReadOnlyList<GroupMemberCount> GroupMemberCounts,
    string UngroupedEntityType,
    int UngroupedCount,
    int TotalClientCountForType);
