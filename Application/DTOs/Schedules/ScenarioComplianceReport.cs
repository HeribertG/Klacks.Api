// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.PeriodClosing;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Compliance verdict for an isolated scenario versus the real plan: the issues the scenario NEWLY
/// introduces (pre-existing real-plan issues are diffed away) and the subset that blocks acceptance
/// because a Block-mode enforcement rule produced an Error.
/// </summary>
/// <param name="NewIssues">Issues present only in the scenario evaluation, not in the real-plan baseline</param>
/// <param name="BlockingIssues">New issues with Severity Error that carry the enforcement-rule param</param>
public sealed record ScenarioComplianceReport(
    IReadOnlyList<PeriodIssueDto> NewIssues,
    IReadOnlyList<PeriodIssueDto> BlockingIssues);
