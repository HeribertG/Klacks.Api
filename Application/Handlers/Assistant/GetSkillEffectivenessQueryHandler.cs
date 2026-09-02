// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// W6.1: builds the "Skill-Wirksamkeit" scorecard from the W1 telemetry tables over the requested
/// reporting window. Pure read-only aggregation; the controller guards it with the admin role and
/// the repository does the grouping, so no persistence type reaches this layer.
/// </summary>
/// <param name="repository">Windowed aggregation over usage records, recipe runs and trajectories</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetSkillEffectivenessQueryHandler
    : IRequestHandler<GetSkillEffectivenessQuery, SkillEffectivenessResource>
{
    private readonly ISkillEffectivenessRepository _repository;

    public GetSkillEffectivenessQueryHandler(ISkillEffectivenessRepository repository)
    {
        _repository = repository;
    }

    public async Task<SkillEffectivenessResource> Handle(
        GetSkillEffectivenessQuery request, CancellationToken cancellationToken)
    {
        var days = Math.Clamp(
            request.Days, SkillEffectivenessDefaults.MinDays, SkillEffectivenessDefaults.MaxDays);
        var from = DateTime.UtcNow.AddDays(-days);

        var evalRuns = await _repository.GetEvalTrendAsync(
            SkillEffectivenessDefaults.EvalTrendLimit, cancellationToken);
        var funnel = await _repository.GetRecipeFunnelAsync(from, cancellationToken);
        var usageCount = await _repository.GetUsageCountAsync(from, cancellationToken);
        var failureCounts = await _repository.GetFailureCountsAsync(from, cancellationToken);
        var callStats = await _repository.GetSkillCallStatsAsync(from, cancellationToken);
        var trajectorySample = await _repository.GetChosenSourceSampleAsync(
            from, SkillEffectivenessDefaults.TrajectorySampleLimit, cancellationToken);

        var stats = BuildSkillStats(callStats);

        return new SkillEffectivenessResource
        {
            Days = days,
            EvalTrend = evalRuns
                .Select(e => new SkillEffectivenessEvalRun
                {
                    Goldset = e.Goldset,
                    Model = e.Model,
                    CompositeScore = e.CompositeScore,
                    ItemsTotal = e.ItemsTotal,
                    ItemsPassed = e.ItemsPassed,
                    CreateTime = e.CreateTime
                })
                .ToList(),
            RecipeFunnel = funnel
                .Select(r => new SkillEffectivenessRecipeFunnelRow
                {
                    RecipeName = r.RecipeName,
                    Started = r.Started,
                    Running = r.Running,
                    Completed = r.Completed,
                    Aborted = r.Aborted,
                    Expired = r.Expired
                })
                .ToList(),
            FailureSummary = BuildFailureSummary(usageCount, failureCounts),
            TopSkills = stats
                .OrderByDescending(s => s.SuccessRate)
                .ThenByDescending(s => s.Calls)
                .ThenBy(s => s.SkillName, StringComparer.Ordinal)
                .Take(SkillEffectivenessDefaults.TopFlopLimit)
                .ToList(),
            FlopSkills = stats
                .Where(s => s.SuccessRate < SkillEffectivenessDefaults.FlopMaxSuccessRate)
                .OrderBy(s => s.SuccessRate)
                .ThenByDescending(s => s.Calls)
                .ThenBy(s => s.SkillName, StringComparer.Ordinal)
                .Take(SkillEffectivenessDefaults.TopFlopLimit)
                .ToList(),
            ChosenSourceDistribution = SkillEffectivenessParser.DistributeChosenSources(
                trajectorySample.Select(t => (t.LlmChosenSkill, t.KnowledgeIndexCandidatesJson)))
        };
    }

    private static List<SkillEffectivenessSkillStat> BuildSkillStats(
        IReadOnlyList<Domain.Models.Assistant.SkillCallStat> callStats)
    {
        return callStats
            .Where(g => g.Calls >= SkillEffectivenessDefaults.TopFlopMinCalls)
            .Select(g => new SkillEffectivenessSkillStat
            {
                SkillName = g.SkillName,
                Calls = g.Calls,
                Failures = g.Failures,
                Successes = g.Calls - g.Failures,
                SuccessRate = g.Calls == 0 ? 0 : (double)(g.Calls - g.Failures) / g.Calls
            })
            .ToList();
    }

    private static SkillEffectivenessFailureSummary BuildFailureSummary(
        int usageCount, IReadOnlyList<Domain.Models.Assistant.SkillFailureKindCount> failureCounts)
    {
        var summary = new SkillEffectivenessFailureSummary { TotalRows = usageCount };

        foreach (var group in failureCounts)
        {
            switch (group.Kind)
            {
                case SkillFailureKind.NotFound: summary.NotFound = group.Count; break;
                case SkillFailureKind.PermissionDenied: summary.PermissionDenied = group.Count; break;
                case SkillFailureKind.ParameterInvalid: summary.ParameterInvalid = group.Count; break;
                case SkillFailureKind.GateHold: summary.GateHold = group.Count; break;
                case SkillFailureKind.UiActionContext: summary.UiActionContext = group.Count; break;
                case SkillFailureKind.Exception: summary.Exception = group.Count; break;
            }
        }

        summary.HallucinationRate = summary.TotalRows == 0
            ? 0
            : (double)summary.NotFound / summary.TotalRows;

        return summary;
    }
}
