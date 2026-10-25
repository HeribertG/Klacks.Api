// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Read-only loader of the master-data facts reported next to a resolved contract value (assignments, rule,
/// settings, monthly table, workdays). Shared by the absence-hours and target-hours calculations.
/// </summary>
public interface IEmployeeContractFactsLoader
{
    /// <param name="contractUsedId">Id of the contract the resolver picked, flagged as used</param>
    /// <param name="viewInterval">Payment interval of the view; null flags nothing as filtered</param>
    Task<IReadOnlyList<AbsenceHoursContractSummary>> SummariseContractsAsync(
        Guid clientId, DateOnly date, Guid? contractUsedId, int? viewInterval);

    Task<AbsenceHoursSchedulingRuleSummary?> LoadRuleAsync(Guid? schedulingRuleId);

    Task<AbsenceHoursSettingsSummary> LoadSettingsAsync(CancellationToken cancellationToken);

    /// <summary>Monthly target hours rows of a year, month -> hours (soft-deleted rows excluded).</summary>
    Task<IReadOnlyDictionary<int, decimal>> LoadMonthlyRowsOfYearAsync(int year);

    /// <summary>Workdays of every month of a year: configured weekend days and official holidays excluded.</summary>
    Task<IReadOnlyDictionary<int, int>> CountWorkdaysPerMonthAsync(int year, Guid? calendarSelectionId, CancellationToken cancellationToken);
}
