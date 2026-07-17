// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IClientOnCallHoursProvider
{
    /// <summary>
    /// Sums a client's WEIGHTED on-call hours (OnCallPresence/OnCallStandby work changes joined to their
    /// parent Work) over a date range, applying the per-category factors from the supplied config. Used
    /// by the PeriodCapEvaluator to subtract on-call hours from the TotalHours cap baseline when on-call
    /// is excluded from the period caps. Uses the same AnalyseToken equality semantics as the other
    /// period-hours queries (exact match, null = real mode); soft-deleted rows are excluded by the EF
    /// query filter. Returns zero when the config is disabled.
    /// </summary>
    /// <param name="clientId">The client whose on-call work changes are summed (attributed to the parent Work's client).</param>
    /// <param name="start">Inclusive first day of the range (matched on the parent Work's CurrentDate).</param>
    /// <param name="end">Inclusive last day of the range.</param>
    /// <param name="analyseToken">Scenario isolation: only rows whose parent Work carries exactly this token (null = real mode).</param>
    /// <param name="config">Resolved on-call config supplying the presence/standby weighting factors.</param>
    Task<decimal> GetWeightedOnCallHoursAsync(Guid clientId, DateOnly start, DateOnly end, Guid? analyseToken, OnCallConfig config);
}
