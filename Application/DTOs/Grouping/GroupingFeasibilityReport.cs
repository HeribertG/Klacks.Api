// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Result of one grouping feasibility analysis: the findings (F1-F7), the ordered proposals, both
/// fingerprints (plan: report findings + proposals + subtree, report: report findings) and the display
/// names the chat report needs. Ids stay in the findings; names are looked up here.
/// GroupLineage holds every group with itself and its ancestors, resolved over Parent like the plan view
/// (the nested-set Root column is unreliable), and MemberGroups the direct groups of every grouped client
/// and shift; both feed the caller's group-scope check. UnitSummaries holds one entry per planning unit
/// (GroupingUnitSummaryBuilder), ordered by unit name.
/// </summary>

namespace Klacks.Api.Application.DTOs.Grouping;

public sealed record GroupingFeasibilityReport(
    GroupingAnalysisRequest Request,
    IReadOnlyList<GroupingFinding> Findings,
    IReadOnlyList<GroupingProposal> Proposals,
    string Fingerprint,
    string ReportFingerprint,
    IReadOnlyDictionary<Guid, string> GroupNames,
    IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> GroupLineage,
    IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> MemberGroups,
    IReadOnlyDictionary<Guid, string> ClientNames,
    IReadOnlyDictionary<Guid, string> ShiftNames,
    int AnalysedClientCount,
    int AnalysedShiftCount)
{
    public IReadOnlyList<GroupingUnitSummary> UnitSummaries { get; init; } = [];

    public bool HasReportFindings => Findings.Any(GroupingFinding.IsReportFinding);
}
