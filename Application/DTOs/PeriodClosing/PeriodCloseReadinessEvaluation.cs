// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.PeriodClosing;

/// <summary>
/// The result of evaluate_period_close_readiness: countable facts, the findings and the server-computed verdict and
/// recommendation, which the model relays and does not rephrase into a different conclusion.
/// </summary>
/// <param name="GroupId">The group the period belongs to</param>
/// <param name="GroupName">Display name of the group</param>
/// <param name="PeriodStart">First day of the period</param>
/// <param name="PeriodEnd">Last day of the period</param>
/// <param name="LagDays">The stored close lag; null when none is stored</param>
/// <param name="EarliestCloseDate">First day the period is due</param>
/// <param name="ErrorCount">Findings of severity Error; 0 when the period was not checked because nothing is to seal</param>
/// <param name="WarningCount">Findings of severity Warning; 0 when the period was not checked because nothing is to seal</param>
/// <param name="SealedDayCount">Days of the period holding a day lock</param>
/// <param name="UnsealedDayCount">Days without a day lock on which the group seal would act</param>
/// <param name="UnsealedDays">The first of those days, at most PeriodCloseReadinessAdvisor.MaxListedUnsealedDays</param>
/// <param name="Verdict">The computed verdict</param>
/// <param name="Findings">Every finding, blocking ones first</param>
/// <param name="Recommendation">The computed recommendation</param>
public sealed record PeriodCloseReadinessEvaluation(
    Guid GroupId,
    string GroupName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int? LagDays,
    DateOnly EarliestCloseDate,
    int ErrorCount,
    int WarningCount,
    int SealedDayCount,
    int UnsealedDayCount,
    IReadOnlyList<DateOnly> UnsealedDays,
    PeriodCloseVerdict Verdict,
    IReadOnlyList<PeriodCloseFinding> Findings,
    string Recommendation);
