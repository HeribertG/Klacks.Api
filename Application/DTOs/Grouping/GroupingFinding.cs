// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One finding of a grouping feasibility analysis (codes F1-F7). IsReportFinding decides which findings
/// belong in the report and the inbox (plan decision D15): F1, F5, F6 only when it cannot be solved by a
/// removal, and F7. F2, F3 and F4 are solved by proposals and never count as report findings.
/// </summary>
/// <param name="Code">Finding code F1-F7.</param>
/// <param name="ReportOnly">True when no proposal solves the finding; distinguishes the two F6 cases.</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Grouping;

public sealed record GroupingFinding(
    GroupingFindingCode Code,
    bool ReportOnly,
    Guid? GroupId = null,
    Guid? ShiftId = null,
    Guid? ClientId = null,
    GroupingIneligibilityReason? Reason = null,
    IReadOnlyList<GroupingReasonCount>? ReasonCounts = null,
    DayOfWeek? Weekday = null,
    int? Demand = null,
    int? Supply = null)
{
    public static bool IsReportFinding(GroupingFinding finding) => finding.Code switch
    {
        GroupingFindingCode.ShiftUnfillableGlobally => true,
        GroupingFindingCode.ClientFitsNoShift => true,
        GroupingFindingCode.CapacityShortfall => true,
        GroupingFindingCode.ClientDeadMembership => finding.ReportOnly,
        _ => false,
    };
}
