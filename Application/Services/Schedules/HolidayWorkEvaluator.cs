// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IHolidayWorkEvaluator"/>. Reports every day a client works on that their
/// calendar marks as a statutory holiday. Until now the holiday rules shipped in the language packs
/// only fed surcharges and the calendar display - no detector ever read them, so planning someone onto
/// a public holiday produced no finding at all.
/// An exemption suppresses the finding: care, security and similar operations run legally on holidays.
/// Exemptions are scoped like PeriodCapRule - a row without a scheduling rule exempts everyone, a row
/// with one exempts only clients whose active contract references that rule (the industry axis).
/// The finding is a Warning and escalates to Error when the holidayWork rule is configured as Block.
/// </summary>
/// <param name="exemptionRepository">Reads the active exemptions</param>
/// <param name="holidayCalendarResolver">Answers which days are holidays for this client</param>
/// <param name="contractDataProvider">Resolves the client's calendar selection and scheduling rule</param>
/// <param name="enforcementResolver">Resolves warn/block for the holidayWork compliance rule</param>

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Scheduling;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class HolidayWorkEvaluator : IHolidayWorkEvaluator
{
    private readonly IHolidayWorkExemptionRuleRepository _exemptionRepository;
    private readonly IClientHolidayCalendarResolver _holidayCalendarResolver;
    private readonly IClientContractDataProvider _contractDataProvider;
    private readonly IComplianceEnforcementResolver _enforcementResolver;

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

        var anchorDate = workDates.Min();
        var contractData = await _contractDataProvider.GetEffectiveContractDataAsync(clientId, anchorDate);

        if (await IsExemptAsync(contractData.SchedulingRuleId))
        {
            return entries;
        }

        var isBlocked = await _enforcementResolver.GetModeAsync(ComplianceRuleNames.HolidayWork) == RuleEnforcementMode.Block;

        foreach (var year in workDates.Select(d => d.Year).Distinct())
        {
            var calculator = await _holidayCalendarResolver.GetCalculatorAsync(contractData.CalendarSelectionId, year);
            if (calculator == null)
            {
                continue;
            }

            foreach (var date in workDates.Where(d => d.Year == year).OrderBy(d => d))
            {
                if (calculator.IsHoliday(date) != HolidayStatus.OfficialHoliday)
                {
                    continue;
                }

                entries.Add(BuildEntry(clientId, clientName, date, calculator.GetHolidayInfo(date)?.CurrentName, isBlocked));
            }
        }

        return entries;
    }

    /// <summary>
    /// True when any active exemption covers this client: a global one, or one bound to the scheduling
    /// rule the client's contract references.
    /// </summary>
    private async Task<bool> IsExemptAsync(Guid? schedulingRuleId)
    {
        var exemptions = await _exemptionRepository.GetAllActiveAsync();
        return exemptions.Any(e => e.SchedulingRuleId == null
            || (schedulingRuleId.HasValue && e.SchedulingRuleId == schedulingRuleId));
    }

    private static ScheduleValidationNotificationDto BuildEntry(
        Guid clientId,
        string clientName,
        DateOnly date,
        string? holidayName,
        bool isBlocked)
    {
        var commentParams = new Dictionary<string, string>
        {
            ["holiday"] = holidayName ?? string.Empty,
        };

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
