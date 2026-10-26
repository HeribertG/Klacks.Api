// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// One contract assignment of the employee that is active on the calculation date, with the fields the
/// absence-hours calculation reads from it. Assignments whose payment interval differs from the interval the
/// calculation ran with are listed too, flagged as ignored - that is the usual reason a contract "does not count".
/// </summary>
/// <param name="ContractId">Contract id</param>
/// <param name="Name">Contract name</param>
/// <param name="PaymentInterval">Payment interval of the contract</param>
/// <param name="UsedByCalculation">True for the one contract the calculation resolved for the day</param>
/// <param name="FilteredByPaymentInterval">True when the view interval filtered this contract out (PaymentIntervalViewMatcher)</param>
/// <param name="GuaranteedHours">Guaranteed hours per interval; null inherits the company value</param>
/// <param name="FullTime">Full-time reference hours; null or 0 falls back to the settings</param>
/// <param name="Percent">Workload percent field; scales only an inherited or monthly basis</param>
/// <param name="SchedulingRuleId">Scheduling rule attached to the contract, if any</param>
public sealed record AbsenceHoursContractSummary(
    Guid ContractId,
    string Name,
    PaymentInterval PaymentInterval,
    bool UsedByCalculation,
    bool FilteredByPaymentInterval,
    decimal? GuaranteedHours,
    decimal? FullTime,
    decimal? Percent,
    Guid? SchedulingRuleId);
