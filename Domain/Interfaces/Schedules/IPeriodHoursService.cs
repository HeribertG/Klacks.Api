// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IPeriodHoursService
{
    Task<Dictionary<Guid, PeriodHoursResource>> GetPeriodHoursAsync(
        List<Guid> clientIds,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken = null);

    Task<PeriodHoursResource> CalculatePeriodHoursAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken = null);

    Task RecalculatePeriodHoursAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken = null);

    Task RecalculateAllClientsAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? groupId = null,
        Guid? analyseToken = null);

    /// <summary>
    /// Recomputes, in place, every cached period-hours row of the given plan (real plan when
    /// <paramref name="analyseToken"/> is null) whose pay period overlaps <paramref name="fromDate"/>..<paramref name="untilDate"/>,
    /// and commits. For bulk writers that bypass the per-work recalculation hooks, e.g. accepting a scenario.
    /// </summary>
    Task RefreshCachedPeriodHoursAsync(
        DateOnly fromDate,
        DateOnly untilDate,
        Guid? analyseToken = null);

    Task InvalidateCacheAsync(
        Guid clientId,
        DateOnly date,
        Guid? analyseToken = null);

    Task<PeriodHoursResource> RecalculateAndNotifyAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken,
        string? excludeConnectionId = null);

    (DateOnly StartDate, DateOnly EndDate) GetPeriodBoundaries(DateOnly date);

    Task<(DateOnly StartDate, DateOnly EndDate)> GetPeriodBoundariesAsync(DateOnly date);

    /// <summary>
    /// Resolves the pay-period boundaries from the contract's own PaymentInterval instead of the
    /// global setting. Required for PaymentInterval.Individual, whose boundaries are only defined
    /// by the IndividualPeriod linked to the contract.
    /// </summary>
    Task<(DateOnly StartDate, DateOnly EndDate)> GetPeriodBoundariesForContractAsync(Guid contractId, DateOnly date);
}
