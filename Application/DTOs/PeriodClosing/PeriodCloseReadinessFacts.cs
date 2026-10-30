// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.PeriodClosing;

/// <summary>
/// The gathered facts about one period of one group, the input of PeriodCloseReadinessAdvisor.
/// </summary>
/// <param name="GroupId">The group the period belongs to</param>
/// <param name="GroupName">Display name of the group</param>
/// <param name="PeriodStart">First day of the period</param>
/// <param name="PeriodEnd">Last day of the period</param>
/// <param name="Today">The company's own local day</param>
/// <param name="LagDays">The stored close lag; null when none is stored</param>
/// <param name="Stop">The first shared gate that stops the period (PeriodCloseGates.FirstStopAsync)</param>
/// <param name="HasWorkOnChildGroups">True when work falls into the period on shifts of descendant groups</param>
/// <param name="ErrorCount">Findings of severity Error, after the Block-rule escalation</param>
/// <param name="WarningCount">Findings of severity Warning, after the Block-rule escalation</param>
/// <param name="SealedDayCount">Days of the period holding a day lock (group or installation-wide)</param>
/// <param name="UnsealedDaysWithWork">Days without a day lock on which the group seal would act, in date order</param>
public sealed record PeriodCloseReadinessFacts(
    Guid GroupId,
    string GroupName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly Today,
    int? LagDays,
    PeriodCloseStop Stop,
    bool HasWorkOnChildGroups,
    int ErrorCount,
    int WarningCount,
    int SealedDayCount,
    IReadOnlyList<DateOnly> UnsealedDaysWithWork);
