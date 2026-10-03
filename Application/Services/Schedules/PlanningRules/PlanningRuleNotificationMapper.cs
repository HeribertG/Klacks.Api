// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Maps a planning-rule finding onto a schedule validation entry with the generic planning-rule key. Hard
/// findings become Error entries tagged with the planningRule enforcement name, so every write gate treats them
/// like the other Block-mode rules (supervisor-overridable); soft findings are Warnings. A team-wide finding has
/// no agent and is reported with ClientId Guid.Empty. Parameters are culture-invariant.
/// </summary>

using System.Globalization;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.ScheduleOptimizer.Constraints.Rules;

namespace Klacks.Api.Application.Services.Schedules.PlanningRules;

public static class PlanningRuleNotificationMapper
{
    public const string KindParam = "kind";
    public const string ObservedParam = "observed";
    public const string LimitParam = "limit";
    public const string RuleIdParam = "ruleId";

    private const string NumberFormat = "0.##";

    public static ScheduleValidationNotificationDto ToNotification(RuleFinding finding, string clientName)
    {
        ArgumentNullException.ThrowIfNull(finding);
        var isHard = finding.Severity == RuleSeverity.Hard;
        var commentParams = new Dictionary<string, string>
        {
            [KindParam] = finding.Kind.ToString(),
            [ObservedParam] = finding.Observed.ToString(NumberFormat, CultureInfo.InvariantCulture),
            [LimitParam] = finding.Limit.ToString(NumberFormat, CultureInfo.InvariantCulture),
            [RuleIdParam] = finding.RuleId.ToString(),
        };
        if (isHard)
        {
            commentParams[ComplianceRuleNames.EnforcementRuleParamKey] = ComplianceRuleNames.PlanningRule;
        }

        return new ScheduleValidationNotificationDto
        {
            Type = isHard ? ScheduleValidationType.Error : ScheduleValidationType.Warning,
            ClientId = ParseClientId(finding.AgentId),
            ClientName = clientName,
            Date = finding.Date,
            Comment = ScheduleValidationKeys.PlanningRule,
            CommentParams = commentParams,
        };
    }

    /// <summary>
    /// Finding for an approved Hard constraint that could not be evaluated because its stored parameters are
    /// invalid: only the rule id, the rule and not a person is broken.
    /// </summary>
    public static ScheduleValidationNotificationDto ToInvalidRuleNotification(Guid ruleId, DateOnly date, ScheduleValidationType type, Guid clientId)
        => new()
        {
            Type = type,
            ClientId = clientId,
            ClientName = string.Empty,
            Date = date,
            Comment = ScheduleValidationKeys.PlanningRuleInvalid,
            CommentParams = new Dictionary<string, string> { [RuleIdParam] = ruleId.ToString() },
        };

    public static Guid ParseClientId(string? agentId)
        => Guid.TryParse(agentId, CultureInfo.InvariantCulture, out var clientId) ? clientId : Guid.Empty;
}
