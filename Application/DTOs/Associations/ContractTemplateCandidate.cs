// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// An existing contract template ranked against the administrator's wishes, with its key values and the exact
/// fields that would have to be adapted when a new contract is created from it.
/// </summary>
/// <param name="Id">Contract id, usable as the template of a new contract</param>
/// <param name="Name">Contract name</param>
/// <param name="GuaranteedHours">Fixed guaranteed hours; null means the contract inherits the company-wide workload</param>
/// <param name="Percent">Workload percent of the inherited company-wide value; null counts as 100</param>
/// <param name="MinimumHours">Minimum hours of the band around the guaranteed hours</param>
/// <param name="MaximumHours">Maximum hours of the band around the guaranteed hours</param>
/// <param name="FullTime">Full-time reference hours; 0 means not configured</param>
/// <param name="PaymentInterval">Payment interval name</param>
/// <param name="PerformsShiftWork">Shift-work flag; null means the standard decides</param>
/// <param name="Workdays">Working weekdays as a comma separated token list</param>
/// <param name="HolidayCalendarName">Name of the contract's own holiday calendar, or a label when it has none</param>
/// <param name="ValidFrom">Validity start of the template</param>
/// <param name="ValidUntil">Validity end of the template; null means open-ended</param>
/// <param name="DifferenceCount">Number of wished fields in which the template differs</param>
/// <param name="Differences">The differing fields with template and wished value</param>
/// <param name="ImpliedChanges">Changes the workload path switch makes beyond the wishes; not counted as differences</param>
namespace Klacks.Api.Application.DTOs.Associations;

public sealed record ContractTemplateCandidate(
    Guid Id,
    string Name,
    decimal? GuaranteedHours,
    decimal? Percent,
    decimal MinimumHours,
    decimal MaximumHours,
    decimal FullTime,
    string PaymentInterval,
    bool? PerformsShiftWork,
    string Workdays,
    string HolidayCalendarName,
    DateTime ValidFrom,
    DateTime? ValidUntil,
    int DifferenceCount,
    IReadOnlyList<ContractTemplateDifference> Differences,
    IReadOnlyList<ContractTemplateImpliedChange> ImpliedChanges);
