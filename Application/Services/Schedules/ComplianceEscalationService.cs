// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IComplianceEscalationService"/>. Holds the timeline-key-to-rule-name map and
/// escalates a rule's Warning entries to Error when the rule's enforcement mode is Block, exactly as
/// the pre-commit checker did before the extraction (behavior-preserving).
/// </summary>
/// <param name="enforcementResolver">Resolves warn/block per rule type for the K1 Block-mode escalation</param>
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class ComplianceEscalationService : IComplianceEscalationService
{
    private static readonly IReadOnlyDictionary<string, string> CommentToRuleName = new Dictionary<string, string>
    {
        [ScheduleValidationKeys.RestViolation] = ComplianceRuleNames.MinRestHours,
        [ScheduleValidationKeys.Overtime] = ComplianceRuleNames.MaxDailyHours,
        [ScheduleValidationKeys.ConsecutiveDays] = ComplianceRuleNames.MaxConsecutiveDays,
        [ScheduleValidationKeys.WeeklyOvertime] = ComplianceRuleNames.MaxWeeklyHours,
        [ScheduleValidationKeys.MinRestDays] = ComplianceRuleNames.MinRestDays,
    };

    private readonly IComplianceEnforcementResolver _enforcementResolver;

    public ComplianceEscalationService(IComplianceEnforcementResolver enforcementResolver)
    {
        _enforcementResolver = enforcementResolver;
    }

    public async Task EscalateBlockedViolationsAsync(List<ScheduleValidationNotificationDto> newConflicts)
    {
        for (var i = 0; i < newConflicts.Count; i++)
        {
            var entry = newConflicts[i];
            if (entry.Type != ScheduleValidationType.Warning
                || !CommentToRuleName.TryGetValue(entry.Comment, out var ruleName))
            {
                continue;
            }

            if (!await IsBlockModeAsync(ruleName))
            {
                continue;
            }

            var escalatedParams = new Dictionary<string, string>(entry.CommentParams)
            {
                [ComplianceRuleNames.EnforcementRuleParamKey] = ruleName,
            };
            newConflicts[i] = entry with { Type = ScheduleValidationType.Error, CommentParams = escalatedParams };
        }
    }

    public async Task EscalateBlockedIssuesAsync(IReadOnlyList<PeriodIssueDto> issues)
    {
        foreach (var issue in issues)
        {
            if (issue.Severity != ScheduleValidationType.Warning
                || !CommentToRuleName.TryGetValue(issue.MessageKey, out var ruleName))
            {
                continue;
            }

            if (!await IsBlockModeAsync(ruleName))
            {
                continue;
            }

            issue.Severity = ScheduleValidationType.Error;
            issue.MessageParams[ComplianceRuleNames.EnforcementRuleParamKey] = ruleName;
        }
    }

    private async Task<bool> IsBlockModeAsync(string ruleName)
        => await _enforcementResolver.GetModeAsync(ruleName) == RuleEnforcementMode.Block;
}
