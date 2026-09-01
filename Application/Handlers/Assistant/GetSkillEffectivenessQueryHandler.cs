// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// W6.1: builds the "Skill-Wirksamkeit" scorecard from the W1 telemetry tables. Pure read-only
/// aggregation; the controller guards it with the admin role.
/// </summary>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetSkillEffectivenessQueryHandler
    : IRequestHandler<GetSkillEffectivenessQuery, SkillEffectivenessResource>
{
    private const int EvalTrendLimit = 20;
    private const int TrajectorySampleLimit = 2000;
    private const int TopFlopLimit = 10;
    private const int TopFlopMinCalls = 5;

    private readonly DataBaseContext _db;

    public GetSkillEffectivenessQueryHandler(DataBaseContext db)
    {
        _db = db;
    }

    public async Task<SkillEffectivenessResource> Handle(
        GetSkillEffectivenessQuery request, CancellationToken cancellationToken)
    {
        var resource = new SkillEffectivenessResource();

        resource.EvalTrend = await _db.EvalRuns
            .OrderByDescending(e => e.CreateTime)
            .Take(EvalTrendLimit)
            .Select(e => new SkillEffectivenessEvalRun
            {
                Goldset = e.Goldset,
                Model = e.Model,
                CompositeScore = e.CompositeScore,
                ItemsTotal = e.ItemsTotal,
                ItemsPassed = e.ItemsPassed,
                CreateTime = e.CreateTime
            })
            .ToListAsync(cancellationToken);

        var recipeGroups = await _db.RecipeRuns
            .GroupBy(r => r.RecipeName)
            .Select(g => new
            {
                RecipeName = g.Key,
                Started = g.Count(),
                Running = g.Count(x => x.Status == RecipeRunStatus.Running),
                Completed = g.Count(x => x.Status == RecipeRunStatus.Completed),
                Aborted = g.Count(x => x.Status == RecipeRunStatus.Aborted),
                Expired = g.Count(x => x.Status == RecipeRunStatus.Expired)
            })
            .ToListAsync(cancellationToken);

        resource.RecipeFunnel = recipeGroups
            .OrderByDescending(r => r.Started)
            .Select(r => new SkillEffectivenessRecipeFunnelRow
            {
                RecipeName = r.RecipeName,
                Started = r.Started,
                Running = r.Running,
                Completed = r.Completed,
                Aborted = r.Aborted,
                Expired = r.Expired
            })
            .ToList();

        var failureSummary = new SkillEffectivenessFailureSummary
        {
            TotalRows = await _db.SkillUsageRecords.CountAsync(cancellationToken)
        };

        var failureGroups = await _db.SkillUsageRecords
            .Where(s => s.FailureKind != null)
            .GroupBy(s => s.FailureKind)
            .Select(g => new { Kind = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        foreach (var group in failureGroups)
        {
            switch (group.Kind)
            {
                case SkillFailureKind.NotFound: failureSummary.NotFound = group.Count; break;
                case SkillFailureKind.PermissionDenied: failureSummary.PermissionDenied = group.Count; break;
                case SkillFailureKind.ParameterInvalid: failureSummary.ParameterInvalid = group.Count; break;
                case SkillFailureKind.GateHold: failureSummary.GateHold = group.Count; break;
                case SkillFailureKind.UiActionContext: failureSummary.UiActionContext = group.Count; break;
                case SkillFailureKind.Exception: failureSummary.Exception = group.Count; break;
            }
        }

        failureSummary.HallucinationRate = failureSummary.TotalRows == 0
            ? 0
            : (double)failureSummary.NotFound / failureSummary.TotalRows;

        resource.FailureSummary = failureSummary;

        var skillGroups = await _db.SkillUsageRecords
            .GroupBy(s => s.SkillName)
            .Select(g => new
            {
                SkillName = g.Key,
                Calls = g.Count(),
                Failures = g.Count(x => !x.Success || x.FailureKind != null)
            })
            .ToListAsync(cancellationToken);

        var stats = skillGroups
            .Where(g => g.Calls >= TopFlopMinCalls)
            .Select(g => new SkillEffectivenessSkillStat
            {
                SkillName = g.SkillName,
                Calls = g.Calls,
                Failures = g.Failures,
                Successes = g.Calls - g.Failures,
                SuccessRate = g.Calls == 0 ? 0 : (double)(g.Calls - g.Failures) / g.Calls
            })
            .ToList();

        resource.TopSkills = stats
            .OrderByDescending(s => s.SuccessRate)
            .ThenByDescending(s => s.Calls)
            .Take(TopFlopLimit)
            .ToList();

        resource.FlopSkills = stats
            .OrderBy(s => s.SuccessRate)
            .ThenByDescending(s => s.Calls)
            .Take(TopFlopLimit)
            .ToList();

        var trajectorySample = await _db.SkillSelectionTrajectories
            .OrderByDescending(t => t.CreateTime)
            .Take(TrajectorySampleLimit)
            .Select(t => new { t.LlmChosenSkill, t.KnowledgeIndexCandidatesJson })
            .ToListAsync(cancellationToken);

        resource.ChosenSourceDistribution = SkillEffectivenessParser.DistributeChosenSources(
            trajectorySample.Select(t => (t.LlmChosenSkill, t.KnowledgeIndexCandidatesJson)));

        return resource;
    }
}
