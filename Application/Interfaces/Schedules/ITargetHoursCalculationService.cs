// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Lays open how the target hours of an employee for a pay period are resolved, on the same pipeline the schedule row
/// header and get_period_hours use (IClientContractDataProvider at the period start, prorated by membership).
/// </summary>
public interface ITargetHoursCalculationService
{
    /// <param name="clientId">Employee</param>
    /// <param name="dayInPeriod">Any day of the pay period (the first of a month for a monthly period)</param>
    /// <param name="viewInterval">Payment interval of the schedule view to flag filtered contracts against; null takes the company setting</param>
    Task<TargetHoursCalculation> CalculateAsync(Guid clientId, DateOnly dayInPeriod, int? viewInterval, CancellationToken cancellationToken = default);

    /// <summary>Company-level facts of a year: monthly table, workdays per month (global calendar) and work settings.</summary>
    Task<(IReadOnlyDictionary<int, decimal> MonthlyRows, IReadOnlyDictionary<int, int> Workdays, AbsenceHoursSettingsSummary Settings)> LoadCompanyFactsAsync(
        int year, CancellationToken cancellationToken = default);
}
