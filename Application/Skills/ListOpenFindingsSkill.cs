// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Chat skill (Etappe 3f of the Klacksy-proactive plan) that reports the open condition-ledger findings
/// visible to the calling planner - what Klacksy's background detectors have already noticed, not a
/// fresh scan. Answers "what's open" / "what has Klacksy found" without waiting for the next proactive
/// notification. Read-only: it only queries AgentCondition rows, it never writes to the ledger.
/// </summary>
/// <param name="limit">Optional. Maximum number of findings to return, most urgent first (default 20).</param>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_open_findings")]
public class ListOpenFindingsSkill : BaseSkillImplementation
{
    private const int DefaultLimit = 20;

    private readonly IAgentConditionRepository _repository;
    private readonly IAgentConditionScopeResolver _scopeResolver;
    private readonly IEnumerable<IAgentTriggerDetector> _detectors;
    private readonly TimeProvider _timeProvider;

    public ListOpenFindingsSkill(
        IAgentConditionRepository repository,
        IAgentConditionScopeResolver scopeResolver,
        IEnumerable<IAgentTriggerDetector> detectors,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _scopeResolver = scopeResolver;
        _detectors = detectors;
        _timeProvider = timeProvider;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var limit = GetParameter<int?>(parameters, "limit") ?? DefaultLimit;
        if (limit < 1)
        {
            limit = DefaultLimit;
        }

        var scope = await _scopeResolver.ResolveAsync(context.UserId.ToString(), cancellationToken);
        if (!scope.IsPlanner)
        {
            return SkillResult.SuccessResult(
                new { TotalOpenFindings = 0, Findings = Array.Empty<object>() },
                "list_open_findings only applies to a planner (Admin/Authorised role); this user has no planning scope, so there is nothing to report.");
        }

        var totalOpenFindings = await _repository.CountOpenForScopeAsync(scope.IsUnrestricted, scope.VisibleRootIds, cancellationToken);
        var conditions = await _repository.GetOpenForScopeAsync(scope.IsUnrestricted, scope.VisibleRootIds, limit, cancellationToken);

        var reconciledKinds = _detectors
            .Where(detector => detector is IAgentConditionFingerprintSource)
            .Select(detector => detector.Kind)
            .ToHashSet(StringComparer.Ordinal);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var findings = conditions.Select(condition => ToFindingData(condition, reconciledKinds, nowUtc)).ToList();
        var staleTrackedCount = findings.Count(finding => !finding.ReconciliationTracked);

        return SkillResult.SuccessResult(
            new { TotalOpenFindings = totalOpenFindings, ShownFindings = findings.Count, Findings = findings },
            BuildMessage(findings.Count, totalOpenFindings, staleTrackedCount));
    }

    private static string BuildMessage(int shownCount, int totalCount, int staleTrackedCount)
    {
        if (totalCount == 0)
        {
            return "No open findings in your visibility scope.";
        }

        var truncatedNote = totalCount > shownCount
            ? $" Showing the {shownCount} most urgent of {totalCount} total."
            : string.Empty;

        var staleNote = staleTrackedCount > 0
            ? $" {staleTrackedCount} of the shown finding(s) carry ReconciliationTracked=false: their detector kind " +
              "does not re-verify every tick that the condition is still true, so treat those as \"last known true\" " +
              "at lastSeenAtUtc, not necessarily still true right now."
            : string.Empty;

        return $"{totalCount} open finding(s) in your visibility scope, sorted by severity then age.{truncatedNote}{staleNote}";
    }

    private static OpenFindingData ToFindingData(AgentCondition condition, IReadOnlySet<string> reconciledKinds, DateTime nowUtc)
    {
        var isReconciled = reconciledKinds.Contains(condition.TriggerKind);
        var ageDays = Math.Max(0, (int)(nowUtc - condition.DetectedAtUtc).TotalDays);

        return new OpenFindingData(
            ConditionId: condition.Id,
            Kind: condition.TriggerKind,
            Severity: condition.Severity,
            Status: condition.Status.ToString(),
            DetectedAtUtc: condition.DetectedAtUtc,
            LastSeenAtUtc: condition.LastSeenAtUtc,
            AgeDays: ageDays,
            EntityId: condition.EntityId,
            GroupId: condition.GroupId,
            AttemptCount: condition.AttemptCount,
            EscalatedAtUtc: condition.EscalatedAtUtc,
            ActionRoute: AgentConditionActionRoutes.For(condition.TriggerKind),
            ReconciliationTracked: isReconciled,
            StalenessNote: isReconciled
                ? null
                : "This kind's open status is not re-verified every tick; it may already be resolved in reality.");
    }

    private sealed record OpenFindingData(
        Guid ConditionId,
        string Kind,
        string Severity,
        string Status,
        DateTime DetectedAtUtc,
        DateTime LastSeenAtUtc,
        int AgeDays,
        Guid? EntityId,
        Guid? GroupId,
        int AttemptCount,
        DateTime? EscalatedAtUtc,
        string? ActionRoute,
        bool ReconciliationTracked,
        string? StalenessNote);
}
