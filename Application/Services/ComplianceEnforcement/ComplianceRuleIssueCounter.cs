// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Counts, for one compliance rule, the findings of a period issue list that a switch to Block concerns: the warnings
/// carrying one of the rule's message keys (they would turn into errors) and the errors tagged with the rule's name
/// (they exist because it already blocks). The issue list is the one the period check returns, escalation applied.
/// </summary>
/// <param name="rule">A rule name of ComplianceRuleCatalog</param>
/// <param name="currentMode">The effective reaction of the rule now</param>
/// <param name="issues">The findings of the window</param>

using Klacks.Api.Application.DTOs.ComplianceEnforcement;
using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ComplianceEnforcement;

public static class ComplianceRuleIssueCounter
{
    public static ComplianceRuleFacts Count(string rule, RuleEnforcementMode currentMode, IReadOnlyList<PeriodIssueDto> issues)
    {
        var keys = ComplianceRuleCatalog.IssueKeys(rule);

        var warnings = issues.Count(issue =>
            issue.Severity == ScheduleValidationType.Warning && keys.Contains(issue.MessageKey));
        var enforcedErrors = issues.Count(issue =>
            issue.Severity == ScheduleValidationType.Error
            && issue.MessageParams.TryGetValue(ComplianceRuleNames.EnforcementRuleParamKey, out var tagged)
            && tagged == rule);

        return new ComplianceRuleFacts(rule, currentMode, warnings, enforcedErrors);
    }
}
