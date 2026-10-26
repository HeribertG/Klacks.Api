// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lays open the target-hours resolution of one employee for one pay period by running the production pipeline: the
/// period boundaries and the prorated period target come from IPeriodHoursService (the same call the schedule row
/// header and get_period_hours make), the provenance of every figure from IClientContractDataProvider resolved at the
/// period start, and the surrounding master data from the shared facts loader. Nothing is reimplemented or written.
/// </summary>
/// <param name="periodHoursService">Period boundaries and the prorated period target of the schedule</param>
/// <param name="contractDataProvider">Production resolver of effective contract data with provenance</param>
/// <param name="facts">Shared loader of assignments, rule, settings, monthly table and workdays</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class TargetHoursCalculationService : ITargetHoursCalculationService
{
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IClientContractDataProvider _contractDataProvider;
    private readonly IEmployeeContractFactsLoader _facts;

    public TargetHoursCalculationService(
        IPeriodHoursService periodHoursService,
        IClientContractDataProvider contractDataProvider,
        IEmployeeContractFactsLoader facts)
    {
        _periodHoursService = periodHoursService;
        _contractDataProvider = contractDataProvider;
        _facts = facts;
    }

    public async Task<TargetHoursCalculation> CalculateAsync(
        Guid clientId, DateOnly dayInPeriod, int? viewInterval, CancellationToken cancellationToken = default)
    {
        var settings = await _facts.LoadSettingsAsync(cancellationToken);
        var view = viewInterval ?? settings.PaymentInterval;
        var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(dayInPeriod);

        // The period target reads the contract WITHOUT a view-interval filter (PeriodHoursService does the same), so the
        // resolution here is the one the schedule shows; the view interval only flags which contracts a booking in that
        // view would not see.
        var contractData = await _contractDataProvider.GetEffectiveContractDataAsync(clientId, periodStart);
        var periodHours = await _periodHoursService.GetPeriodHoursAsync([clientId], periodStart, periodEnd, null);
        var periodTarget = periodHours.TryGetValue(clientId, out var hours) ? hours.GuaranteedHours : contractData.GuaranteedHours;

        var monthlyRows = await _facts.LoadMonthlyRowsOfYearAsync(periodStart.Year);

        return new TargetHoursCalculation
        {
            ClientId = clientId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            ViewInterval = view,
            ContractData = contractData,
            Way = ClassifyWay(contractData),
            ContractsActiveAtPeriodStart = await _facts.SummariseContractsAsync(clientId, periodStart, contractData.ContractId, view),
            SchedulingRule = await _facts.LoadRuleAsync(contractData.SchedulingRuleId),
            Settings = settings,
            MonthlyTargetHoursRow = monthlyRows.TryGetValue(periodStart.Month, out var row) ? row : null,
            MonthlyTargetHoursOfYear = monthlyRows,
            WorkdaysPerMonth = await _facts.CountWorkdaysPerMonthAsync(periodStart.Year, contractData.CalendarSelectionId, cancellationToken),
            PeriodTargetHours = periodTarget
        };
    }

    public async Task<(IReadOnlyDictionary<int, decimal> MonthlyRows, IReadOnlyDictionary<int, int> Workdays, AbsenceHoursSettingsSummary Settings)> LoadCompanyFactsAsync(
        int year, CancellationToken cancellationToken = default)
    {
        return (
            await _facts.LoadMonthlyRowsOfYearAsync(year),
            await _facts.CountWorkdaysPerMonthAsync(year, null, cancellationToken),
            await _facts.LoadSettingsAsync(cancellationToken));
    }

    // Mirrors ClientContractDataProvider: the interval decides Weg 1, an inherited basis (percent-scaled) Weg 2,
    // everything else is a fixed contract.
    public static TargetHoursWay ClassifyWay(EffectiveContractData data)
    {
        if (!data.HasActiveContract)
        {
            return TargetHoursWay.NoContract;
        }

        if (data.PaymentInterval == (int)PaymentInterval.MonthlyTargetHours)
        {
            return TargetHoursWay.MonthlyTableInterval;
        }

        return data.GuaranteedHoursScaledByContractPercent ? TargetHoursWay.InheritedCompanyValue : TargetHoursWay.FixedHours;
    }
}
