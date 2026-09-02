// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core aggregation for the "Skill-Wirksamkeit" scorecard (W6). Groups server-side so the handler
/// never sees a DbContext and the whole telemetry history never travels into memory.
/// </summary>
/// <param name="context">Telemetry tables; soft-deleted rows are filtered by the model configuration</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class SkillEffectivenessRepository : ISkillEffectivenessRepository
{
    private readonly DataBaseContext _context;

    public SkillEffectivenessRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EvalRun>> GetEvalTrendAsync(
        int limit, CancellationToken cancellationToken = default)
    {
        return await _context.EvalRuns
            .OrderByDescending(e => e.CreateTime)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecipeFunnelRow>> GetRecipeFunnelAsync(
        DateTime from, CancellationToken cancellationToken = default)
    {
        var rows = await _context.RecipeRuns
            .Where(r => r.CreateTime >= from)
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

        return rows
            .OrderByDescending(r => r.Started)
            .ThenBy(r => r.RecipeName, StringComparer.Ordinal)
            .Select(r => new RecipeFunnelRow(
                r.RecipeName, r.Started, r.Running, r.Completed, r.Aborted, r.Expired))
            .ToList();
    }

    public async Task<int> GetUsageCountAsync(DateTime from, CancellationToken cancellationToken = default)
    {
        return await _context.SkillUsageRecords
            .Where(s => s.CreateTime >= from)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SkillFailureKindCount>> GetFailureCountsAsync(
        DateTime from, CancellationToken cancellationToken = default)
    {
        var rows = await _context.SkillUsageRecords
            .Where(s => s.CreateTime >= from && s.FailureKind != null)
            .GroupBy(s => s.FailureKind!.Value)
            .Select(g => new { Kind = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new SkillFailureKindCount(r.Kind, r.Count)).ToList();
    }

    public async Task<IReadOnlyList<SkillCallStat>> GetSkillCallStatsAsync(
        DateTime from, CancellationToken cancellationToken = default)
    {
        var rows = await _context.SkillUsageRecords
            .Where(s => s.CreateTime >= from
                && (s.UiActionStatus == null || s.UiActionStatus != UiActionStatus.Dispatched))
            .GroupBy(s => s.SkillName)
            .Select(g => new
            {
                SkillName = g.Key,
                Calls = g.Count(),
                Failures = g.Count(x => !x.Success || x.FailureKind != null)
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new SkillCallStat(r.SkillName, r.Calls, r.Failures)).ToList();
    }

    public async Task<IReadOnlyList<TrajectoryChosenSourceSample>> GetChosenSourceSampleAsync(
        DateTime from, int limit, CancellationToken cancellationToken = default)
    {
        var rows = await _context.SkillSelectionTrajectories
            .Where(t => t.CreateTime >= from)
            .OrderByDescending(t => t.CreateTime)
            .Take(limit)
            .Select(t => new { t.LlmChosenSkill, t.KnowledgeIndexCandidatesJson })
            .ToListAsync(cancellationToken);

        return rows
            .Select(t => new TrajectoryChosenSourceSample(t.LlmChosenSkill, t.KnowledgeIndexCandidatesJson))
            .ToList();
    }
}
