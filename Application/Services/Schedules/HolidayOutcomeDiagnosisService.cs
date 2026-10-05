// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IHolidayOutcomeDiagnosisService"/>. Every verdict comes from a production service, never from
/// a copy of its logic: the contract of the day from IClientContractDataProvider, the holidays from
/// IClientHolidayCalendarResolver (selection entries and "reminder only" override applied), the surcharge flag from
/// the same IsPaidOfficialHoliday predicate MacroDataProvider uses for works, and the warning from
/// IHolidayWorkEvaluator itself. The service only adds the explanation: which step of the chain decided it.
/// Whether a non-official day was downgraded by a "reminder only" entry is found by recomputing the selection's own
/// rules without the override.
/// </summary>
/// <param name="contractDataProvider">Effective contract (calendar, rate, scheduling rule) on the date</param>
/// <param name="holidayCalendarResolver">Production holiday calculator of the contract calendar</param>
/// <param name="holidayCalendarSourceResolver">Names the calendar and where it comes from</param>
/// <param name="holidayWorkEvaluator">Production holiday-work warning</param>
/// <param name="exemptionRepository">Active holiday-work exemptions, for naming the one that applies</param>
/// <param name="enforcementResolver">Warn or block for the holiday-work rule</param>
/// <param name="calendarSelectionRepository">Selection entries, to detect a "reminder only" downgrade</param>
/// <param name="settingsRepository">Calendar rules, to detect a "reminder only" downgrade</param>
/// <param name="contractRepository">Contract name</param>
/// <param name="schedulingRuleRepository">Name of the scheduling rule an exemption is bound to</param>
/// <param name="workCoverageReader">Works of the person touching the date, from the production timeline</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.CalendarSelections;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Services.Holidays;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class HolidayOutcomeDiagnosisService : IHolidayOutcomeDiagnosisService
{
    private readonly IClientContractDataProvider _contractDataProvider;
    private readonly IClientHolidayCalendarResolver _holidayCalendarResolver;
    private readonly IHolidayCalendarSourceResolver _holidayCalendarSourceResolver;
    private readonly IHolidayWorkEvaluator _holidayWorkEvaluator;
    private readonly IHolidayWorkExemptionRuleRepository _exemptionRepository;
    private readonly IComplianceEnforcementResolver _enforcementResolver;
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IContractRepository _contractRepository;
    private readonly ISchedulingRuleRepository _schedulingRuleRepository;
    private readonly IClientWorkCoverageReader _workCoverageReader;

    public HolidayOutcomeDiagnosisService(
        IClientContractDataProvider contractDataProvider,
        IClientHolidayCalendarResolver holidayCalendarResolver,
        IHolidayCalendarSourceResolver holidayCalendarSourceResolver,
        IHolidayWorkEvaluator holidayWorkEvaluator,
        IHolidayWorkExemptionRuleRepository exemptionRepository,
        IComplianceEnforcementResolver enforcementResolver,
        ICalendarSelectionRepository calendarSelectionRepository,
        ISettingsRepository settingsRepository,
        IContractRepository contractRepository,
        ISchedulingRuleRepository schedulingRuleRepository,
        IClientWorkCoverageReader workCoverageReader)
    {
        _contractDataProvider = contractDataProvider;
        _holidayCalendarResolver = holidayCalendarResolver;
        _holidayCalendarSourceResolver = holidayCalendarSourceResolver;
        _holidayWorkEvaluator = holidayWorkEvaluator;
        _exemptionRepository = exemptionRepository;
        _enforcementResolver = enforcementResolver;
        _calendarSelectionRepository = calendarSelectionRepository;
        _settingsRepository = settingsRepository;
        _contractRepository = contractRepository;
        _schedulingRuleRepository = schedulingRuleRepository;
        _workCoverageReader = workCoverageReader;
    }

    public async Task<HolidayOutcomeDiagnosis> DiagnoseAsync(
        Guid clientId,
        string clientName,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var contract = await _contractDataProvider.GetEffectiveContractDataAsync(clientId, date);
        var calendar = await _holidayCalendarSourceResolver.ResolveAsync(contract.CalendarSelectionId, cancellationToken);
        var calculator = await _holidayCalendarResolver.GetCalculatorAsync(contract.CalendarSelectionId, date.Year);

        var status = calculator?.IsHoliday(date) ?? HolidayStatus.NotAHoliday;
        var holiday = calculator?.GetHolidayInfo(date);
        var earnsSurcharge = calculator?.IsPaidOfficialHoliday(date) ?? false;
        var downgraded = status == HolidayStatus.UnofficialHoliday
                         && await IsOfficialWithoutReminderOverrideAsync(calendar, date);

        var exemption = await FindExemptionAsync(contract.SchedulingRuleId);
        var warningEntries = await _holidayWorkEvaluator.EvaluateAsync(clientId, clientName, [date], cancellationToken);
        var warningRaised = warningEntries.Count > 0;

        var dayReason = DayReason(calculator, status, downgraded);
        var surchargeReason = SurchargeReason(dayReason, status, earnsSurcharge, contract.HolidayRate);
        var warningReason = WarningReason(dayReason, exemption, warningRaised);

        return new HolidayOutcomeDiagnosis
        {
            Date = date,
            HasActiveContract = contract.HasActiveContract,
            ContractName = await ContractNameAsync(contract.ContractId),
            Calendar = calendar,
            HolidayName = holiday?.Name,
            Status = status,
            DowngradedByReminderOnly = downgraded,
            EarnsHolidayTimeSurcharge = earnsSurcharge,
            HolidayRate = contract.HolidayRate,
            SurchargeReason = surchargeReason,
            HolidayWorkWarningRaised = warningRaised,
            WarningReason = warningReason,
            ExemptionDescription = exemption?.Description,
            ExemptionSchedulingRuleName = await SchedulingRuleNameAsync(exemption?.SchedulingRuleId),
            EnforcementMode = await _enforcementResolver.GetModeAsync(ComplianceRuleNames.HolidayWork),
            WorksOnDate = await _workCoverageReader.GetWorksTouchingAsync(clientId, date, cancellationToken),
        };
    }

    private static string? DayReason(IHolidaysListCalculator? calculator, HolidayStatus status, bool downgraded)
    {
        if (calculator == null)
        {
            return HolidayOutcomeReasonCodes.NoHolidayCalendar;
        }

        return status switch
        {
            HolidayStatus.NotAHoliday => HolidayOutcomeReasonCodes.NotAHoliday,
            HolidayStatus.UnofficialHoliday => downgraded
                ? HolidayOutcomeReasonCodes.ReminderOnlyEntry
                : HolidayOutcomeReasonCodes.RuleNotOfficial,
            _ => null,
        };
    }

    private static string SurchargeReason(string? dayReason, HolidayStatus status, bool earnsSurcharge, decimal holidayRate)
    {
        if (dayReason != null)
        {
            return dayReason;
        }

        if (status == HolidayStatus.OfficialHoliday && !earnsSurcharge)
        {
            return HolidayOutcomeReasonCodes.NotMarkedForTimeSurcharge;
        }

        return holidayRate == 0m ? HolidayOutcomeReasonCodes.HolidayRateZero : HolidayOutcomeReasonCodes.Applies;
    }

    private static string WarningReason(string? dayReason, HolidayWorkExemptionRule? exemption, bool warningRaised)
    {
        var explained = dayReason ?? (exemption != null ? HolidayOutcomeReasonCodes.ExemptionApplies : HolidayOutcomeReasonCodes.Applies);
        var explainedRaised = explained == HolidayOutcomeReasonCodes.Applies;

        return explainedRaised == warningRaised ? explained : HolidayOutcomeReasonCodes.Unexplained;
    }

    private async Task<HolidayWorkExemptionRule?> FindExemptionAsync(Guid? schedulingRuleId)
    {
        var exemptions = await _exemptionRepository.GetAllActiveAsync();
        return exemptions.FirstOrDefault(e => e.SchedulingRuleId == null)
               ?? exemptions.FirstOrDefault(e => schedulingRuleId.HasValue && e.SchedulingRuleId == schedulingRuleId);
    }

    private async Task<bool> IsOfficialWithoutReminderOverrideAsync(ResolvedHolidayCalendarSource calendar, DateOnly date)
    {
        if (calendar.CalendarSelectionId == null)
        {
            return false;
        }

        var selection = await _calendarSelectionRepository.GetNoTrackingWithSelectedCalendars(calendar.CalendarSelectionId.Value);
        if (selection == null || selection.SelectedCalendars.All(entry => entry.OfficialOverride != false))
        {
            return false;
        }

        var pairs = selection.SelectedCalendars.Select(entry => (entry.Country, entry.State)).ToHashSet();
        var rules = (await _settingsRepository.GetCalendarRuleList())
            .Where(rule => pairs.Contains((rule.Country, rule.State)))
            .ToList();

        var withoutOverride = new HolidaysListCalculator { CurrentYear = date.Year };
        withoutOverride.AddRange(rules);
        withoutOverride.ComputeHolidays();
        return withoutOverride.IsHoliday(date) == HolidayStatus.OfficialHoliday;
    }

    private async Task<string?> ContractNameAsync(Guid? contractId)
    {
        if (contractId == null)
        {
            return null;
        }

        return (await _contractRepository.GetNoTracking(contractId.Value))?.Name;
    }

    private async Task<string?> SchedulingRuleNameAsync(Guid? schedulingRuleId)
    {
        if (schedulingRuleId == null)
        {
            return null;
        }

        return (await _schedulingRuleRepository.GetNoTracking(schedulingRuleId.Value))?.Name;
    }
}
