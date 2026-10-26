// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the master-data facts the "lay the calculation open" skills report next to a resolved contract value: the
/// contract assignments active on a day (flagged against the view interval), the scheduling rule, the company work
/// settings, the monthly target hours rows of a year and the workdays of every month. Shared by the absence-hours and
/// the target-hours calculation so both name the same facts the same way. Read-only.
/// </summary>
/// <param name="clientRepository">Loads the employee with the contract assignments</param>
/// <param name="schedulingRuleRepository">Loads the scheduling rule attached to the resolved contract</param>
/// <param name="monthlyTargetHoursRepository">Monthly target hours rows</param>
/// <param name="settingsReader">Company work settings</param>
/// <param name="weekConfiguration">The company's weekend days, for the workdays per month</param>
/// <param name="holidayCalendarResolver">Holiday calendar of the contract (or the global one), for the workdays per month</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Associations;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class EmployeeContractFactsLoader : IEmployeeContractFactsLoader
{
    private readonly IClientRepository _clientRepository;
    private readonly ISchedulingRuleRepository _schedulingRuleRepository;
    private readonly IMonthlyTargetHoursRepository _monthlyTargetHoursRepository;
    private readonly ISettingsReader _settingsReader;
    private readonly IWeekConfiguration _weekConfiguration;
    private readonly IClientHolidayCalendarResolver _holidayCalendarResolver;

    public EmployeeContractFactsLoader(
        IClientRepository clientRepository,
        ISchedulingRuleRepository schedulingRuleRepository,
        IMonthlyTargetHoursRepository monthlyTargetHoursRepository,
        ISettingsReader settingsReader,
        IWeekConfiguration weekConfiguration,
        IClientHolidayCalendarResolver holidayCalendarResolver)
    {
        _clientRepository = clientRepository;
        _schedulingRuleRepository = schedulingRuleRepository;
        _monthlyTargetHoursRepository = monthlyTargetHoursRepository;
        _settingsReader = settingsReader;
        _weekConfiguration = weekConfiguration;
        _holidayCalendarResolver = holidayCalendarResolver;
    }

    public static bool ReadsMonthlyTable(EffectiveContractData contractData) =>
        !contractData.HasActiveContract
        || contractData.PaymentInterval == (int)PaymentInterval.MonthlyTargetHours
        || contractData.GuaranteedHoursScaledByContractPercent;

    public async Task<IReadOnlyList<AbsenceHoursContractSummary>> SummariseContractsAsync(
        Guid clientId, DateOnly date, Guid? contractUsedId, int? viewInterval)
    {
        var client = await _clientRepository.Get(clientId);
        if (client == null)
        {
            return [];
        }

        return client.ClientContracts
            .Where(assignment => assignment.IsActive
                && !assignment.IsDeleted
                && assignment.FromDate <= date
                && (assignment.UntilDate == null || assignment.UntilDate >= date)
                && assignment.Contract != null)
            .OrderByDescending(assignment => assignment.FromDate)
            .ThenByDescending(assignment => assignment.CreateTime)
            .ThenBy(assignment => assignment.Id)
            .Select(assignment => new AbsenceHoursContractSummary(
                assignment.ContractId,
                assignment.Contract.Name,
                assignment.Contract.PaymentInterval,
                assignment.ContractId == contractUsedId,
                viewInterval is { } view && !PaymentIntervalViewMatcher.Matches(assignment.Contract.PaymentInterval, view),
                assignment.Contract.GuaranteedHours,
                assignment.Contract.FullTime,
                assignment.Contract.Percent,
                assignment.Contract.SchedulingRuleId))
            .ToList();
    }

    public async Task<AbsenceHoursSchedulingRuleSummary?> LoadRuleAsync(Guid? schedulingRuleId)
    {
        if (schedulingRuleId is not { } ruleId)
        {
            return null;
        }

        var rule = await _schedulingRuleRepository.GetNoTracking(ruleId);
        return rule == null
            ? null
            : new AbsenceHoursSchedulingRuleSummary(
                rule.Id, rule.Name, rule.GuaranteedHours, rule.FullTimeHours, rule.DefaultWorkingHours);
    }

    public async Task<AbsenceHoursSettingsSummary> LoadSettingsAsync(CancellationToken cancellationToken)
    {
        var values = await _settingsReader.GetSettingsByTypesAsync(
            [SettingKeys.GuaranteedHours, SettingKeys.FullTime, SettingKeys.DefaultWorkingHours, SettingKeys.PaymentInterval],
            cancellationToken);

        return new AbsenceHoursSettingsSummary(
            ParseDecimal(values.GetValueOrDefault(SettingKeys.GuaranteedHours)),
            ParseDecimal(values.GetValueOrDefault(SettingKeys.FullTime)),
            ParseDecimal(values.GetValueOrDefault(SettingKeys.DefaultWorkingHours)),
            ParseNullableInt(values.GetValueOrDefault(SettingKeys.PaymentInterval)));
    }

    public async Task<IReadOnlyDictionary<int, decimal>> LoadMonthlyRowsOfYearAsync(int year)
    {
        var rows = await _monthlyTargetHoursRepository.ListByYear(year);
        return rows
            .Where(row => !row.IsDeleted)
            .GroupBy(row => row.Month)
            .ToDictionary(group => group.Key, group => group.First().Hours);
    }

    // The same holiday rule as the absence macros (official holidays of the applicable calendar) and the same
    // configured weekend, so the workdays agree with the days an absence macro credits.
    public async Task<IReadOnlyDictionary<int, int>> CountWorkdaysPerMonthAsync(
        int year, Guid? calendarSelectionId, CancellationToken cancellationToken)
    {
        var weekendDays = await _weekConfiguration.GetWeekendDaysAsync(cancellationToken);
        var calculator = await _holidayCalendarResolver.GetCalculatorAsync(calendarSelectionId, year);
        Func<DateOnly, bool>? isHoliday = calculator == null
            ? null
            : date => calculator.IsHoliday(date) == HolidayStatus.OfficialHoliday;

        return Enumerable.Range(MonthlyTargetHoursConstants.MinimumMonth, MonthlyTargetHoursConstants.MaximumMonth)
            .ToDictionary(month => month, month => MonthWorkdayCounter.Count(year, month, weekendDays, isHoliday));
    }

    private static decimal ParseDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : decimal.Zero;

    // No company interval configured means the Ui books without an interval filter; the calculations do the same.
    private static int? ParseNullableInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
}
