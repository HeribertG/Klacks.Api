// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

public sealed record GroupingReportView(
    string From,
    string Until,
    string Period,
    string? Group,
    string PlanCode,
    GroupingFeasibilityCounts Counts,
    GroupingProposalKindCounts ProposalCounts,
    int AnalysedEmployees,
    int AnalysedDuties,
    IReadOnlyList<GroupingFindingView> Findings,
    IReadOnlyList<GroupingProposalView> Proposals,
    IReadOnlyList<GroupingOmittedCount> Omitted,
    GroupingUnitSection PlanningUnits);
