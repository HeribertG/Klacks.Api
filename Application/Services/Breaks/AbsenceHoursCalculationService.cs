// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lays open the absence-hours calculation of one employee on one day by running the production pipeline on a
/// transient entry: the same effective contract data, the same macro input builder and the same macro the booking
/// path uses (BreakMacroService). Nothing is reimplemented and nothing is written - the transient entry is never
/// attached to a DbContext. The provenance of every figure comes from the contract data provider itself.
/// </summary>
/// <param name="facts">Shared loader of assignments, rule, settings, monthly table and workdays</param>
/// <param name="absenceRepository">Absence types, for the name lookup and the default type</param>
/// <param name="breakRepository">Existing absence entries of the employee on the day</param>
/// <param name="contractDataProvider">Production resolver of effective contract data (hours, percent, sources)</param>
/// <param name="macroDataProvider">Production builder of the macro input of an absence entry</param>
/// <param name="macroCompilationService">Production compiler and runner of the absence type's macro</param>
/// <param name="macroManagementService">Names the macro</param>
/// <param name="monthlyTargetHoursRepository">Monthly target hours row of the calculation month</param>

using Klacks.Api.Application.Services.Schedules;
using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Absences;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Services.Breaks;

public sealed class AbsenceHoursCalculationService : IAbsenceHoursCalculationService
{
    private const string DefaultAbsenceEnglishName = "Vacation";

    private readonly IEmployeeContractFactsLoader _facts;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IClientContractDataProvider _contractDataProvider;
    private readonly IMacroDataProvider _macroDataProvider;
    private readonly IMacroCompilationService _macroCompilationService;
    private readonly IMacroManagementService _macroManagementService;
    private readonly IMonthlyTargetHoursRepository _monthlyTargetHoursRepository;

    public AbsenceHoursCalculationService(
        IEmployeeContractFactsLoader facts,
        IAbsenceRepository absenceRepository,
        IBreakRepository breakRepository,
        IClientContractDataProvider contractDataProvider,
        IMacroDataProvider macroDataProvider,
        IMacroCompilationService macroCompilationService,
        IMacroManagementService macroManagementService,
        IMonthlyTargetHoursRepository monthlyTargetHoursRepository)
    {
        _facts = facts;
        _absenceRepository = absenceRepository;
        _breakRepository = breakRepository;
        _contractDataProvider = contractDataProvider;
        _macroDataProvider = macroDataProvider;
        _macroCompilationService = macroCompilationService;
        _macroManagementService = macroManagementService;
        _monthlyTargetHoursRepository = monthlyTargetHoursRepository;
    }

    public async Task<(Absence? Absence, string? Error)> ResolveAbsenceAsync(
        string? absenceName, Guid clientId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var absences = await _absenceRepository.List();

        if (!string.IsNullOrWhiteSpace(absenceName))
        {
            var wanted = absenceName.Trim();
            var byName = absences
                .Where(absence => AbsenceNameMatcher.Matches(absence, wanted))
                .ToList();

            return byName.Count switch
            {
                1 => (byName[0], null),
                0 => (null, $"No absence type named '{wanted}'. Available: {AbsenceNameMatcher.DescribeNames(absences)}."),
                _ => (null, $"Several absence types match '{wanted}': {AbsenceNameMatcher.DescribeNames(byName)}. Use the exact name.")
            };
        }

        var existing = await _breakRepository.GetByClientAndDateRangeAsync(clientId, date, date, cancellationToken);
        var existingAbsence = existing
            .OrderBy(entry => entry.StartTime)
            .Select(entry => absences.FirstOrDefault(absence => absence.Id == entry.AbsenceId))
            .FirstOrDefault(absence => absence != null);
        if (existingAbsence != null)
        {
            return (existingAbsence, null);
        }

        var vacation = absences.FirstOrDefault(absence =>
            string.Equals(absence.Name.En?.Trim(), DefaultAbsenceEnglishName, StringComparison.OrdinalIgnoreCase));
        return vacation != null
            ? (vacation, null)
            : (null, $"No vacation absence type exists; give the absence type by name. Available: {AbsenceNameMatcher.DescribeNames(absences)}.");
    }

    public async Task<AbsenceHoursCalculation> CalculateAsync(
        Guid clientId, DateOnly date, Absence absence, int? paymentInterval, CancellationToken cancellationToken = default)
    {
        var settings = await _facts.LoadSettingsAsync(cancellationToken);
        var intervalUsed = paymentInterval ?? settings.PaymentInterval;

        var contractData = await _contractDataProvider.GetEffectiveContractDataAsync(clientId, date, intervalUsed);
        var contracts = await _facts.SummariseContractsAsync(clientId, date, contractData.ContractId, intervalUsed);
        var rule = await _facts.LoadRuleAsync(contractData.SchedulingRuleId);
        var monthlyRow = await _monthlyTargetHoursRepository.GetByYearMonth(date.Year, date.Month);

        var transientEntry = BuildTransientEntry(clientId, date, absence.Id);
        var macroInput = await _macroDataProvider.GetMacroDataForBreakAsync(transientEntry, intervalUsed);

        var readsMonthlyTable = EmployeeContractFactsLoader.ReadsMonthlyTable(contractData);
        var monthlyRowsOfYear = readsMonthlyTable ? await _facts.LoadMonthlyRowsOfYearAsync(date.Year) : null;
        var workdaysPerMonth = readsMonthlyTable
            ? await _facts.CountWorkdaysPerMonthAsync(date.Year, contractData.CalendarSelectionId, cancellationToken)
            : null;

        var hasMacro = absence.MacroId is { } macroId && macroId != Guid.Empty;
        var macroResult = hasMacro ? await ExecuteMacroAsync(absence.MacroId!.Value, macroInput) : null;

        return new AbsenceHoursCalculation
        {
            ClientId = clientId,
            Date = date,
            AbsenceId = absence.Id,
            AbsenceName = absence.Name,
            MacroId = hasMacro ? absence.MacroId : null,
            MacroName = hasMacro ? await LoadMacroNameAsync(absence.MacroId!.Value) : null,
            PaymentIntervalUsed = intervalUsed,
            ContractData = contractData,
            ContractsActiveOnDate = contracts,
            SchedulingRule = rule,
            Settings = settings,
            MonthlyTargetHoursRow = monthlyRow?.Hours,
            MonthlyTargetHoursOfYear = monthlyRowsOfYear,
            WorkdaysPerMonth = workdaysPerMonth,
            MacroInput = macroInput,
            IsWeekendDay = IsWeekendDay(macroInput),
            MacroExecuted = macroResult is { Success: true },
            ResultHours = macroResult is { Success: true } ? macroResult.ResultValue : null,
            ExistingEntry = await LoadExistingEntryAsync(clientId, date, absence.Id, cancellationToken)
        };
    }

    // The schedule grid books a full-day absence as 00:00-23:59 with 0 hours; the macro then starts from the
    // effective daily working hours. The entry is never added to a DbContext.
    private static Break BuildTransientEntry(Guid clientId, DateOnly date, Guid absenceId) => new()
    {
        Id = Guid.NewGuid(),
        ClientId = clientId,
        AbsenceId = absenceId,
        CurrentDate = date,
        StartTime = TimeOnly.MinValue,
        EndTime = DayTimeConstants.LastMinuteOfDay,
        WorkTime = decimal.Zero
    };

    private static bool IsWeekendDay(MacroData macroInput) =>
        macroInput.Weekday == macroInput.WeekendDay1
        || macroInput.Weekday == macroInput.WeekendDay2
        || macroInput.Weekday == macroInput.WeekendDay3;

    // A missing macro row is reported as "not executed" instead of failing the whole calculation; the booking path
    // swallows the same failure and leaves the booked hours unchanged.
    private async Task<MacroExecutionResult?> ExecuteMacroAsync(Guid macroId, MacroData macroInput)
    {
        try
        {
            return await _macroCompilationService.CompileAndExecuteAsync(macroId, macroInput);
        }
        catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException)
        {
            return null;
        }
    }

    private async Task<string?> LoadMacroNameAsync(Guid macroId)
    {
        try
        {
            var macro = await _macroManagementService.GetMacroAsync(macroId);
            return macro?.Name;
        }
        catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException)
        {
            return null;
        }
    }

    private async Task<AbsenceHoursExistingEntry?> LoadExistingEntryAsync(
        Guid clientId, DateOnly date, Guid absenceId, CancellationToken cancellationToken)
    {
        var entries = await _breakRepository.GetByClientAndDateRangeAsync(clientId, date, date, cancellationToken);
        var entry = entries
            .Where(candidate => candidate.AbsenceId == absenceId && candidate.ParentWorkId == null)
            .OrderBy(candidate => candidate.StartTime)
            .FirstOrDefault();

        return entry == null
            ? null
            : new AbsenceHoursExistingEntry(
                entry.Id, entry.AbsenceId, entry.WorkTime, BreakDurationPredicate.HasDirectlyRecordedDuration(entry), entry.LockLevel);
    }
}
