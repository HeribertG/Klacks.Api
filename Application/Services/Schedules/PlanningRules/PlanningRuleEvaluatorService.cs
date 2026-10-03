// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IPlanningRuleEvaluatorService"/>: the API adapter from persisted Work rows to the shared
/// planning-rule evaluator. Loads the approved PlanningConstraint rules (never CounterRule, that family is
/// reported by CounterRuleEvaluator) through the single loader, reads the Work rows of the evaluation window
/// plus the neighbour days the rules look at, builds one RuleEvaluationContext and maps the findings onto
/// schedule validation entries. The pre-commit path widens the window by the rule horizon on both sides,
/// because RestAfterKind only counts rest days inside the evaluated period, and evaluates the plan before and
/// after the write on the same context. Breaks and WorkChange replacements are not read: a break counts as free,
/// as in every rule consumer. An invalid approved Hard constraint throws PlanningRuleConfigurationException
/// from the loader (fail closed); callers decide whether to propagate it.
/// </summary>
/// <param name="ruleSetLoader">Approved planning rules, agents (night window, workload)</param>
/// <param name="dataReader">Persisted Work rows of the evaluated clients</param>

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.ScheduleOptimizer.Constraints.Rules;

namespace Klacks.Api.Application.Services.Schedules.PlanningRules;

public sealed class PlanningRuleEvaluatorService : IPlanningRuleEvaluatorService
{
    // No sequence rule looks further than this beyond its period (MaxRun and WithinDays are capped at
    // MaxDayDistanceLimit, RestAfterKind reads FreeDays + 1). The service reads that neighbourhood itself, so it
    // declares it as covered and the loader reads no carry-in on top.
    private const int SelfLoadedBoundaryDays = PlanningConstraintDefaults.MaxDayDistanceLimit + 1;

    private readonly IPlanningRuleSetLoader _ruleSetLoader;
    private readonly IPlanningRuleDataReader _dataReader;

    public PlanningRuleEvaluatorService(IPlanningRuleSetLoader ruleSetLoader, IPlanningRuleDataReader dataReader)
    {
        _ruleSetLoader = ruleSetLoader;
        _dataReader = dataReader;
    }

    public async Task<List<ScheduleValidationNotificationDto>> EvaluateRangeAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        IReadOnlyDictionary<Guid, string> clientNames,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientNames);
        var window = await PrepareAsync(clientIds, from, until, extendByHorizon: false, includeTeamFairness: true, analyseToken, cancellationToken);
        if (window is null)
        {
            return [];
        }

        var evaluation = window.Evaluator.Evaluate(window.BuildPlan(window.Segments));
        return evaluation.Findings
            .Select(finding => PlanningRuleNotificationMapper.ToNotification(finding, NameOf(finding, clientNames)))
            .ToList();
    }

    public async Task<List<ScheduleValidationNotificationDto>> EvaluateDayAsync(
        Guid clientId,
        string clientName,
        DateOnly date,
        Guid? analyseToken,
        CancellationToken cancellationToken = default)
    {
        var window = await PrepareAsync([clientId], date, date, extendByHorizon: true, includeTeamFairness: false, analyseToken, cancellationToken);
        if (window is null)
        {
            return [];
        }

        var evaluation = window.Evaluator.Evaluate(window.BuildPlan(window.Segments));
        return evaluation.Findings
            .Where(finding => finding.Date == date)
            .Select(finding => PlanningRuleNotificationMapper.ToNotification(finding, clientName))
            .ToList();
    }

    public async Task<List<ScheduleValidationNotificationDto>> EvaluatePlannedChangeAsync(
        IReadOnlyList<PlannedWorkRow> plannedRows,
        IReadOnlyList<PlannedRemovalRow> removals,
        Guid? analyseToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plannedRows);
        ArgumentNullException.ThrowIfNull(removals);
        if (plannedRows.Count == 0 && removals.Count == 0)
        {
            return [];
        }

        var clientIds = plannedRows.Select(r => r.ClientId).Concat(removals.Select(r => r.ClientId)).Distinct().ToList();
        var dates = plannedRows.Select(r => r.Date).Concat(removals.Select(r => r.Date)).ToList();
        var window = await PrepareAsync(clientIds, dates.Min(), dates.Max(), extendByHorizon: true, includeTeamFairness: false, analyseToken, cancellationToken);
        if (window is null)
        {
            return [];
        }

        var before = window.Evaluator.Evaluate(window.BuildPlan(window.Segments));
        var afterSegments = window.Spans
            .Where(span => !IsRemoved(span, removals))
            .Select(PlanningRuleCarryInLoader.ToSegment)
            .Concat(plannedRows.Select(ToSegment))
            .ToList();
        var after = window.Evaluator.Evaluate(window.BuildPlan(afterSegments));

        return PlanningRuleFindingDelta.NewOrWorsened(before.Findings, after.Findings)
            .Select(finding => PlanningRuleNotificationMapper.ToNotification(finding, string.Empty))
            .ToList();
    }

    private async Task<EvaluationWindow?> PrepareAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly coreFrom,
        DateOnly coreUntil,
        bool extendByHorizon,
        bool includeTeamFairness,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(clientIds);
        if (clientIds.Count == 0 || coreUntil < coreFrom)
        {
            return null;
        }

        var ruleSet = await _ruleSetLoader.LoadRuleSetAsync(
            clientIds,
            coreFrom,
            coreUntil,
            analyseToken,
            SelfLoadedBoundaryDays,
            PlanningRuleSources.PlanningConstraints,
            cancellationToken);
        var rules = includeTeamFairness
            ? ruleSet.Rules
            : ruleSet.Rules.Where(rule => rule is not TeamFairnessRule).ToList();
        if (rules.Count == 0)
        {
            return null;
        }

        var neighborDays = PlanRuleHorizon.NeighborDays(rules);
        var evaluationFrom = extendByHorizon ? coreFrom.AddDays(-neighborDays) : coreFrom;
        var evaluationUntil = extendByHorizon ? coreUntil.AddDays(neighborDays) : coreUntil;
        var spans = await _dataReader.GetWorkSpansAsync(
            ruleSet.Agents.Select(agent => Guid.Parse(agent.Id)).ToList(),
            evaluationFrom.AddDays(-neighborDays),
            evaluationUntil.AddDays(neighborDays),
            analyseToken,
            cancellationToken);
        var segments = spans.Select(PlanningRuleCarryInLoader.ToSegment).ToList();
        var context = new RuleEvaluationContext(evaluationFrom, evaluationUntil, ruleSet.Agents, segments);
        return new EvaluationWindow(PlanRuleEvaluatorFactory.Create(rules, context), context, spans, segments);
    }

    private static string NameOf(RuleFinding finding, IReadOnlyDictionary<Guid, string> clientNames)
        => clientNames.TryGetValue(PlanningRuleNotificationMapper.ParseClientId(finding.AgentId), out var name) ? name : string.Empty;

    private static bool IsRemoved(PlanningRuleWorkSpan span, IReadOnlyList<PlannedRemovalRow> removals)
        => removals.Any(removal => removal.ClientId == span.ClientId
            && (removal.WorkId is not null
                ? removal.WorkId == span.WorkId
                : removal.Date == span.Date && removal.StartTime == span.StartTime && removal.EndTime == span.EndTime));

    private static RuleSegment ToSegment(PlannedWorkRow row)
        => PlanningRuleCarryInLoader.ToSegment(new PlanningRuleWorkSpan(row.ClientId, row.Date, row.StartTime, row.EndTime, 0m));

    private sealed record EvaluationWindow(
        IPlanRuleEvaluator Evaluator,
        RuleEvaluationContext Context,
        IReadOnlyList<PlanningRuleWorkSpan> Spans,
        IReadOnlyList<RuleSegment> Segments)
    {
        public RulePlan BuildPlan(IEnumerable<RuleSegment> segments)
        {
            var plan = new RulePlan(Context);
            foreach (var segment in segments)
            {
                plan.TryAdd(segment);
            }

            return plan;
        }
    }
}
