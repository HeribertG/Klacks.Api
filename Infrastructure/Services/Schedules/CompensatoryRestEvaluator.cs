// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="ICompensatoryRestEvaluator"/> (K12 stage 1). Reads the client's open obligations
/// and emits one entry each: <see cref="ScheduleValidationKeys.CompensatoryRestOverdue"/> once the
/// deadline has passed, otherwise <see cref="ScheduleValidationKeys.CompensatoryRestDue"/>. Warning by
/// default; escalates to Error (tagged with the enforcement-rule param) when the compensatoryRest rule is
/// in Block mode. Returns nothing when the feature is disabled or in scenario mode (analyseToken set), so
/// disabling the feature both stops new obligations and stops surfacing stale ones. The evaluation for several
/// clients reads the enabled flag once, the open obligations of all clients in one repository call and the mode
/// once, and builds the same entries per client.
/// </summary>
/// <param name="obligationRepository">Reads the open (unfulfilled) obligation set for the client</param>
/// <param name="enforcementResolver">Resolves warn/block for the compensatoryRest compliance rule</param>
/// <param name="settingsReader">Reads the enabled flag that gates the whole feed contribution</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
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
        if (analyseToken is not null || !await IsEnabledAsync())
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

    public async Task<List<ScheduleValidationNotificationDto>> EvaluateForClientsAsync(
        IReadOnlyList<ClientAsOfDate> clients,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default)
    {
        if (clients.Count == 0 || analyseToken is not null || !await IsEnabledAsync())
        {
            return [];
        }

        var openByClient = await _obligationRepository.GetOpenByClientsAsync(
            clients.Select(client => client.ClientId).Distinct().ToList(), cancellationToken);
        if (openByClient.Count == 0)
        {
            return [];
        }

        var mode = await _enforcementResolver.GetModeAsync(ComplianceRuleNames.CompensatoryRest);
        var isBlocked = mode == RuleEnforcementMode.Block;

        var entries = new List<ScheduleValidationNotificationDto>();
        foreach (var client in clients)
        {
            if (openByClient.TryGetValue(client.ClientId, out var open))
            {
                entries.AddRange(open.Select(o => BuildEntry(client.ClientId, string.Empty, client.AsOfDate, o, isBlocked)));
            }
        }

        return entries;
    }

    private async Task<bool> IsEnabledAsync()
    {
        var enabledSetting = await _settingsReader.GetSetting(SettingKeys.ComplianceCompensatoryRestEnabled);
        return string.Equals(enabledSetting?.Value, TrueValue, StringComparison.OrdinalIgnoreCase);
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
            ["triggerDate"] = obligation.TriggerDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
            ["dueDate"] = obligation.DueDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
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
