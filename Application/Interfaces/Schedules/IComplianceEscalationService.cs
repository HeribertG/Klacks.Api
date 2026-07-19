// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.PeriodClosing;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// K1 Block-mode escalation: maps a timeline-rule violation (rest, daily/weekly overtime,
/// consecutive days, min rest days) to its compliance rule name and escalates it from Warning to
/// Error when that rule's enforcement mode is Block, tagging it with the enforcement-rule param key
/// so callers can distinguish an overridable block from a structural error. Entries that are already
/// Error or whose comment is not a timeline rule are left untouched.
/// </summary>
public interface IComplianceEscalationService
{
    /// <summary>
    /// Escalates matching Warning entries in place (list slots are replaced with escalated copies).
    /// </summary>
    Task EscalateBlockedViolationsAsync(List<ScheduleValidationNotificationDto> newConflicts);

    /// <summary>
    /// Same escalation over the period-closing issue shape: matching Warning issues are mutated to
    /// Severity Error and tagged with the enforcement-rule message param.
    /// </summary>
    Task EscalateBlockedIssuesAsync(IReadOnlyList<PeriodIssueDto> issues);
}
