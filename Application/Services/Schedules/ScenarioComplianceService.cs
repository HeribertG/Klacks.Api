// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IScenarioComplianceService"/>. Loads the period validation issues once with
/// the scenario token and once for the real plan (both untruncated so blocking entries can never be
/// cut off), escalates timeline warnings per the Block-mode enforcement configuration on both sides,
/// then diffs by Code|Date|ClientId|Params so only issues the scenario NEWLY introduces remain.
/// Blocking issues are the new Error entries tagged with the enforcement-rule param key; structural
/// errors (collisions) surface in NewIssues but are governed by the caller's own acceptance policy.
/// </summary>
/// <param name="validationLoader">End-state period validation engine (same one that closes a period)</param>
/// <param name="escalationService">Escalates timeline warnings to errors for Block-mode rules</param>
using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces.PeriodClosing;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class ScenarioComplianceService : IScenarioComplianceService
{
    private const int UntruncatedIssueLimit = int.MaxValue;

    private readonly IPeriodValidationLoader _validationLoader;
    private readonly IComplianceEscalationService _escalationService;

    public ScenarioComplianceService(
        IPeriodValidationLoader validationLoader,
        IComplianceEscalationService escalationService)
    {
        _validationLoader = validationLoader;
        _escalationService = escalationService;
    }

    public async Task<ScenarioComplianceReport> EvaluateAsync(
        DateOnly from,
        DateOnly until,
        Guid? groupId,
        Guid analyseToken,
        CancellationToken cancellationToken)
    {
        var scenarioIssues = await _validationLoader.LoadAsync(
            from, until, groupId, analyseToken, UntruncatedIssueLimit, cancellationToken);
        var baselineIssues = await _validationLoader.LoadAsync(
            from, until, groupId, null, UntruncatedIssueLimit, cancellationToken);

        // Escalate BOTH sides before diffing: the same rule mode applies to both loads, so an issue
        // present in both worlds stays key-identical after escalation and is still diffed away.
        await _escalationService.EscalateBlockedIssuesAsync(scenarioIssues);
        await _escalationService.EscalateBlockedIssuesAsync(baselineIssues);

        var baselineKeys = baselineIssues.Select(BuildKey).ToHashSet();
        var newIssues = scenarioIssues
            .Where(issue => !baselineKeys.Contains(BuildKey(issue)))
            .ToList();

        var blockingIssues = newIssues
            .Where(issue => issue.Severity == ScheduleValidationType.Error
                && issue.MessageParams.ContainsKey(ComplianceRuleNames.EnforcementRuleParamKey))
            .ToList();

        return new ScenarioComplianceReport(newIssues, blockingIssues);
    }

    // Mirrors PreCommitConflictChecker.BuildKey: params are part of the key so distinct same-day
    // findings stay separate, and a worsened aggregate bucket (different totals) counts as NEW.
    private static string BuildKey(PeriodIssueDto issue)
    {
        var paramSignature = string.Join(
            ",",
            issue.MessageParams.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}"));
        return $"{issue.Code}|{issue.Date:O}|{issue.ClientId}|{paramSignature}";
    }
}
