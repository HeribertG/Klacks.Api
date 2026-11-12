// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IHolidayWorkEvaluator"/>. Reports every day a client works on that their
/// calendar marks as a statutory holiday. Until now the holiday rules shipped in the language packs
/// only fed surcharges and the calendar display - no detector ever read them, so planning someone onto
/// a public holiday produced no finding at all.
/// An exemption suppresses the finding: care, security and similar operations run legally on holidays.
/// Exemptions are scoped like PeriodCapRule - a row without a scheduling rule exempts everyone, a row
/// with one exempts only clients whose active contract references that rule (the industry axis).
/// The finding is a Warning and escalates to Error when the holidayWork rule is configured as Block.
/// The holiday name travels as a MultiLanguage parameter: the finding is broadcast to every planner of the
/// group, each in their own language, so it is localized at display time, not here.
/// The evaluation for several clients judges each client with the same per-day test; it reads the contract data
/// once per distinct first-to-last day range, the mode once, and asks the calendar resolver once per calendar
/// selection and year of the call.
/// </summary>
/// <param name="exemptionRepository">Reads the active exemptions</param>
/// <param name="holidayCalendarResolver">Answers which days are holidays for this client</param>
/// <param name="contractDataProvider">Resolves the client's calendar selection and scheduling rule</param>
/// <param name="enforcementResolver">Resolves warn/block for the holidayWork compliance rule</param>

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.CalendarSelections;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class HolidayWorkEvaluator : IHolidayWorkEvaluator
{
    private readonly IHolidayWorkExemptionRuleRepository _exemptionRepository;
    private readonly IClientHolidayCalendarResolver _holidayCalendarResolver;
    private readonly IClientContractDataProvider _contractDataProvider;
    private readonly IComplianceEnforcementResolver _enforcementResolver;

    // Scoped service: this cache lives exactly as long as the check that created it.
    private IReadOnlyList<HolidayWorkExemptionRule>? _exemptions;

    public HolidayWorkEvaluator(
        IHolidayWorkExemptionRuleRepository exemptionRepository,
        IClientHolidayCalendarResolver holidayCalendarResolver,
        IClientContractDataProvider contractDataProvider,
        IComplianceEnforcementResolver enforcementResolver)
    {
        _exemptionRepository = exemptionRepository;
        _holidayCalendarResolver = holidayCalendarResolver;
        _contractDataProvider = contractDataProvider;
        _enforcementResolver = enforcementResolver;
    }

    public async Task<List<ScheduleValidationNotificationDto>> EvaluateAsync(
        Guid clientId,
        string clientName,
        IReadOnlyCollection<DateOnly> workDates,
        CancellationToken cancellationToken = default)
    {
        var entries = new List<ScheduleValidationNotificationDto>();
        if (workDates.Count == 0)
        {
            return entries;
        }

        // Contract data is resolved PER DAY, not once for the whole set: a contract can change inside
        // the evaluated range, and with it both the scheduling rule that decides the exemption and the
        // calendar that decides what counts as a holiday. Judging late days by the first day's contract
        // would let an ended exemption keep suppressing findings, and a new one not apply yet.
        var orderedDates = workDates.Distinct().OrderBy(d => d).ToList();
        var contractByDate = await _contractDataProvider.GetEffectiveContractDataForClientsRangeAsync(
            [clientId], orderedDates[0], orderedDates[^1]);

        var exemptions = await GetExemptionsAsync();
        var isBlocked = await _enforcementResolver.GetModeAsync(ComplianceRuleNames.HolidayWork) == RuleEnforcementMode.Block;

        await AddEntriesAsync(
            entries, clientId, clientName, orderedDates, contractByDate, exemptions, isBlocked,
            _holidayCalendarResolver.GetCalculatorAsync);

        return entries;
    }

    public async Task<List<ScheduleValidationNotificationDto>> EvaluateForClientsAsync(
        IReadOnlyList<ClientWorkDates> clients,
        CancellationToken cancellationToken = default)
    {
        var entries = new List<ScheduleValidationNotificationDto>();
        var planned = clients
            .Where(client => client.WorkDates.Count > 0)
            .Select(client => (client.ClientId, OrderedDates: client.WorkDates.Distinct().OrderBy(d => d).ToList()))
            .ToList();
        if (planned.Count == 0)
        {
            return entries;
        }

        var contractByRange = new Dictionary<(DateOnly From, DateOnly Until), Dictionary<DateOnly, Dictionary<Guid, EffectiveContractData>>>();
        foreach (var range in planned.GroupBy(client => (From: client.OrderedDates[0], Until: client.OrderedDates[^1]), client => client.ClientId))
        {
            contractByRange[range.Key] = await _contractDataProvider.GetEffectiveContractDataForClientsRangeAsync(
                range.Distinct().ToList(), range.Key.From, range.Key.Until);
        }

        var exemptions = await GetExemptionsAsync();
        var isBlocked = await _enforcementResolver.GetModeAsync(ComplianceRuleNames.HolidayWork) == RuleEnforcementMode.Block;

        var calculators = new Dictionary<(Guid? CalendarSelectionId, int Year), IHolidaysListCalculator?>();
        async Task<IHolidaysListCalculator?> CalculatorOfAsync(Guid? calendarSelectionId, int year)
        {
            if (!calculators.TryGetValue((calendarSelectionId, year), out var calculator))
            {
                calculator = await _holidayCalendarResolver.GetCalculatorAsync(calendarSelectionId, year);
                calculators[(calendarSelectionId, year)] = calculator;
            }

            return calculator;
        }

        foreach (var (clientId, orderedDates) in planned)
        {
            await AddEntriesAsync(
                entries, clientId, string.Empty, orderedDates, contractByRange[(orderedDates[0], orderedDates[^1])],
                exemptions, isBlocked, CalculatorOfAsync);
        }

        return entries;
    }

    private static async Task AddEntriesAsync(
        List<ScheduleValidationNotificationDto> entries,
        Guid clientId,
        string clientName,
        IReadOnlyList<DateOnly> orderedDates,
        IReadOnlyDictionary<DateOnly, Dictionary<Guid, EffectiveContractData>> contractByDate,
        IReadOnlyList<HolidayWorkExemptionRule> exemptions,
        bool isBlocked,
        Func<Guid?, int, Task<IHolidaysListCalculator?>> calculatorOf)
    {
        foreach (var date in orderedDates)
        {
            if (!contractByDate.TryGetValue(date, out var perClient)
                || !perClient.TryGetValue(clientId, out var contractData))
            {
                continue;
            }

            if (IsExempt(exemptions, contractData.SchedulingRuleId))
            {
                continue;
            }

            var calculator = await calculatorOf(contractData.CalendarSelectionId, date.Year);
            if (calculator == null || calculator.IsHoliday(date) != HolidayStatus.OfficialHoliday)
            {
                continue;
            }

            entries.Add(BuildEntry(clientId, clientName, date, calculator.GetHolidayInfo(date)?.Name, isBlocked));
        }
    }

    /// <summary>
    /// Exemptions are read once per scope rather than once per client: the range check calls this
    /// evaluator for every client of the period, on every hub join and every period change, and the
    /// table holds a handful of rows that would otherwise be re-queried each time.
    /// </summary>
    private async Task<IReadOnlyList<HolidayWorkExemptionRule>> GetExemptionsAsync()
    {
        return _exemptions ??= await _exemptionRepository.GetAllActiveAsync();
    }

    /// <summary>
    /// True when any active exemption covers this client: a global one, or one bound to the scheduling
    /// rule the client's contract references on that day.
    /// </summary>
    private static bool IsExempt(IReadOnlyList<HolidayWorkExemptionRule> exemptions, Guid? schedulingRuleId)
    {
        return exemptions.Any(e => e.SchedulingRuleId == null
            || (schedulingRuleId.HasValue && e.SchedulingRuleId == schedulingRuleId));
    }

    private static ScheduleValidationNotificationDto BuildEntry(
        Guid clientId,
        string clientName,
        DateOnly date,
        MultiLanguage? holidayName,
        bool isBlocked)
    {
        var commentParams = new Dictionary<string, string>();
        LocalizedCommentParams.Add(commentParams, LocalizedCommentParamKeys.Holiday, holidayName ?? new MultiLanguage());

        if (isBlocked)
        {
            commentParams[ComplianceRuleNames.EnforcementRuleParamKey] = ComplianceRuleNames.HolidayWork;
        }

        return new ScheduleValidationNotificationDto
        {
            Type = isBlocked ? ScheduleValidationType.Error : ScheduleValidationType.Warning,
            ClientId = clientId,
            ClientName = clientName,
            Date = date,
            Comment = ScheduleValidationKeys.HolidayWork,
            CommentParams = commentParams,
        };
    }
}
