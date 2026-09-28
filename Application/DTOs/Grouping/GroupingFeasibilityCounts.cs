// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Counts of the report's most relevant finding codes, shown in the inbox sentence without loading the
/// full report. From(report) counts the whole analysis; the overload counts a subset, e.g. the findings
/// and proposals a group-restricted user may see.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Grouping;

public sealed record GroupingFeasibilityCounts(int UnfillableShifts, int UnmatchedClients, int CapacityShortfalls, int Proposals)
{
    public static GroupingFeasibilityCounts From(GroupingFeasibilityReport report) =>
        From(report.Findings, report.Proposals.Count);

    public static GroupingFeasibilityCounts From(IReadOnlyCollection<GroupingFinding> findings, int proposalCount) => new(
        findings.Count(f => f.Code == GroupingFindingCode.ShiftUnfillableGlobally),
        findings.Count(f => f.Code == GroupingFindingCode.ClientFitsNoShift),
        findings.Count(f => f.Code == GroupingFindingCode.CapacityShortfall),
        proposalCount);
}
