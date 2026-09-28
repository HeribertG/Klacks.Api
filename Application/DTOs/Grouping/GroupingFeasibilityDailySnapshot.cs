// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The once-per-company-day cached result the grouping feasibility detector compares against, kept
/// small so it fits the in-memory cache without holding the full report. The same shape, built uncached
/// from the visible part of a report, carries a requester's scoped view into their inbox entry.
/// </summary>

namespace Klacks.Api.Application.DTOs.Grouping;

public sealed record GroupingFeasibilityDailySnapshot(string ReportFingerprint, GroupingFeasibilityCounts Counts, bool HasReportFindings)
{
    public static GroupingFeasibilityDailySnapshot From(GroupingFeasibilityReport report) =>
        new(report.ReportFingerprint, GroupingFeasibilityCounts.From(report), report.HasReportFindings);
}
