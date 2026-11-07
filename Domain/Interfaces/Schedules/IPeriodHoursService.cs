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

    /// <summary>
    /// Recalculates and stores the client's row of this pay period. A frozen period (IFrozenPeriodPolicy) is left
    /// unchanged unless <paramref name="includeFrozen"/> is set (admin override, which also lifts the guard for the
    /// removal of overlapping rows with other boundaries).
    /// </summary>
    Task RecalculatePeriodHoursAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken = null,
        bool includeFrozen = false);

    /// <summary>
    /// Recalculates and stores the rows of this pay period for every member (of the group) whose membership overlaps it.
    /// Clients whose period is frozen are skipped unless <paramref name="includeFrozen"/> is set (admin override, which
    /// lifts the guard for the whole call). Returns the frozen clients of the period, skipped or included.
    /// </summary>
    Task<IReadOnlySet<Guid>> RecalculateAllClientsAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? groupId = null,
        Guid? analyseToken = null,
        bool includeFrozen = false);

    /// <summary>
    /// The clients RecalculateAllClientsAsync would consider for this pay period (same population) whose period is frozen,
    /// without writing anything.
    /// </summary>
    Task<IReadOnlySet<Guid>> GetFrozenClientIdsAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? groupId = null,
        Guid? analyseToken = null);

    /// <summary>
    /// The clients a group-restricted caller may recalculate in the window: the population of RecalculateAllClientsAsync
    /// (members of the group whose membership overlaps the pay period, within the caller's group visibility), united over
    /// the real pay periods the window touches. Null when the caller is unrestricted (admin, installation without groups,
    /// background job without a user), so the recalculation needs no client scope. An empty list means the caller may
    /// recalculate nobody; callers must not pass it on as "no scope".
    /// </summary>
    Task<IReadOnlyList<Guid>?> GetRestrictedCallerClientIdsAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? groupId = null);

    /// <summary>
    /// Recomputes, in place, every cached period-hours row of the given plan (real plan when
    /// <paramref name="analyseToken"/> is null) whose pay period overlaps <paramref name="fromDate"/>..<paramref name="untilDate"/>,
    /// and commits. Frozen rows stay unchanged. For bulk writers that bypass the per-work recalculation hooks, e.g.
    /// accepting a scenario.
    /// </summary>
    Task RefreshCachedPeriodHoursAsync(
        DateOnly fromDate,
        DateOnly untilDate,
        Guid? analyseToken = null);

    /// <summary>
    /// Removes the client's cached rows whose period contains <paramref name="date"/>, except frozen rows: the payroll
    /// export reads closed periods only from this cache.
    /// </summary>
    Task InvalidateCacheAsync(
        Guid clientId,
        DateOnly date,
        Guid? analyseToken = null);

    /// <summary>
    /// Recalculates, stores and pushes the client's period hours. For a frozen period nothing is written or pushed and
    /// the cached value is returned (GetPeriodHoursAsync, which calculates without storing when no row exists).
    /// </summary>
    Task<PeriodHoursResource> RecalculateAndNotifyAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken,
        string? excludeConnectionId = null);

    (DateOnly StartDate, DateOnly EndDate) GetPeriodBoundaries(DateOnly date);

    Task<(DateOnly StartDate, DateOnly EndDate)> GetPeriodBoundariesAsync(DateOnly date);

    /// <summary>
    /// The real pay periods (GetPeriodBoundariesAsync) that the range <paramref name="fromDate"/>..<paramref name="untilDate"/>
    /// touches, in date order: the period of <paramref name="fromDate"/> first, then each following period from the day after
    /// the previous period's end, until a period ends on or after <paramref name="untilDate"/>. The first period may start
    /// before <paramref name="fromDate"/> and the last may end after <paramref name="untilDate"/>. Empty when
    /// <paramref name="fromDate"/> is after <paramref name="untilDate"/>. Throws InvalidRequestException when the range spans
    /// more than PayPeriodLimits.MaxPeriodsInRange periods. A cached period-hours row must be keyed by one of these periods,
    /// never by an event or merged window.
    /// </summary>
    Task<IReadOnlyList<(DateOnly StartDate, DateOnly EndDate)>> GetPeriodsInRangeAsync(DateOnly fromDate, DateOnly untilDate);

    /// <summary>
    /// Resolves the pay-period boundaries from the contract's own PaymentInterval instead of the
    /// global setting. Required for PaymentInterval.Individual, whose boundaries are only defined
    /// by the IndividualPeriod linked to the contract.
    /// </summary>
    Task<(DateOnly StartDate, DateOnly EndDate)> GetPeriodBoundariesForContractAsync(Guid contractId, DateOnly date);
}
