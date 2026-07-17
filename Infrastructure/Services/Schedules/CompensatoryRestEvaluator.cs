// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="ICompensatoryRestEvaluator"/> (K12 stage 1). Reads the client's open obligations
/// and emits one entry each: <see cref="ScheduleValidationKeys.CompensatoryRestOverdue"/> once the
/// deadline has passed, otherwise <see cref="ScheduleValidationKeys.CompensatoryRestDue"/>. Warning by
/// default; escalates to Error (tagged with the enforcement-rule param) when the compensatoryRest rule is
/// in Block mode. Returns nothing when the feature is disabled or in scenario mode (analyseToken set), so
/// disabling the feature both stops new obligations and stops surfacing stale ones.
/// </summary>
/// <param name="obligationRepository">Reads the open (unfulfilled) obligation set for the client</param>
/// <param name="enforcementResolver">Resolves warn/block for the compensatoryRest compliance rule</param>
/// <param name="settingsReader">Reads the enabled flag that gates the whole feed contribution</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class CompensatoryRestEvaluator : ICompensatoryRestEvaluator
{
    private const string TrueValue = "true";
    private const string DateFormat = "yyyy-MM-dd";

    private readonly ICompensatoryRestObligationRepository _obligationRepository;
    private readonly IComplianceEnforcementResolver _enforcementResolver;
    private readonly ISettingsReader _settingsReader;

    public CompensatoryRestEvaluator(
        ICompensatoryRestObligationRepository obligationRepository,
        IComplianceEnforcementResolver enforcementResolver,
        ISettingsReader settingsReader)
    {
        _obligationRepository = obligationRepository;
        _enforcementResolver = enforcementResolver;
        _settingsReader = settingsReader;
    }

    public async Task<List<ScheduleValidationNotificationDto>> EvaluateAsync(
        Guid clientId,
        string clientName,
        DateOnly asOfDate,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default)
    {
        if (analyseToken is not null)
        {
            return [];
        }

        var enabledSetting = await _settingsReader.GetSetting(SettingKeys.ComplianceCompensatoryRestEnabled);
        if (!string.Equals(enabledSetting?.Value, TrueValue, StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var open = await _obligationRepository.GetOpenByClientAsync(clientId);
        if (open.Count == 0)
        {
            return [];
        }

        var mode = await _enforcementResolver.GetModeAsync(ComplianceRuleNames.CompensatoryRest);
        var isBlocked = mode == RuleEnforcementMode.Block;

        return open.Select(o => BuildEntry(clientId, clientName, asOfDate, o, isBlocked)).ToList();
    }

    private static ScheduleValidationNotificationDto BuildEntry(
        Guid clientId,
        string clientName,
        DateOnly asOfDate,
        CompensatoryRestObligation obligation,
        bool isBlocked)
    {
        var isOverdue = obligation.DueDate < asOfDate;
        var commentParams = new Dictionary<string, string>
        {
            ["shortfallHours"] = obligation.ShortfallHours.ToString("F1", CultureInfo.InvariantCulture),
            ["triggerDate"] = obligation.TriggerDate.ToString(DateFormat, CultureInfo.InvariantCulture),
            ["dueDate"] = obligation.DueDate.ToString(DateFormat, CultureInfo.InvariantCulture),
        };
        if (isBlocked)
        {
            commentParams[ComplianceRuleNames.EnforcementRuleParamKey] = ComplianceRuleNames.CompensatoryRest;
        }

        return new ScheduleValidationNotificationDto
        {
            Type = isBlocked ? ScheduleValidationType.Error : ScheduleValidationType.Warning,
            ClientId = clientId,
            ClientName = clientName,
            Date = obligation.TriggerDate,
            Comment = isOverdue
                ? ScheduleValidationKeys.CompensatoryRestOverdue
                : ScheduleValidationKeys.CompensatoryRestDue,
            CommentParams = commentParams,
        };
    }
}
