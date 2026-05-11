// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads a JSON goldset, runs each entry against the knowledge retrieval service and stores an EvalRun
/// with composite score, per-dimension breakdown and regression-vs-baseline. Read-only — does not modify
/// skills, descriptions or prompts.
/// </summary>
/// <param name="retrieval">Knowledge index retrieval service</param>
/// <param name="evalRunRepository">Repository for persisting eval runs</param>
/// <param name="goldsetLoader">Loads goldsets by name</param>
/// <param name="logger">Logger</param>

using System.Diagnostics;
using System.Text.Json;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.KnowledgeIndex.Application.Constants;
using Klacks.Api.Infrastructure.KnowledgeIndex.Application.Interfaces;

namespace Klacks.Api.Application.Services.Assistant.Evaluation;

public class EvalRunnerService : IEvalRunnerService
{
    private const double Top1Weight = 0.6;
    private const double Top3Weight = 0.3;
    private const double LatencyWeight = 0.1;
    private const double LatencyNormalizerMs = 1500.0;
    private const int TopK = 3;

    private readonly IKnowledgeRetrievalService _retrieval;
    private readonly IEvalRunRepository _evalRunRepository;
    private readonly IGoldsetLoader _goldsetLoader;
    private readonly ILogger<EvalRunnerService> _logger;

    public EvalRunnerService(
        IKnowledgeRetrievalService retrieval,
        IEvalRunRepository evalRunRepository,
        IGoldsetLoader goldsetLoader,
        ILogger<EvalRunnerService> logger)
    {
        _retrieval = retrieval;
        _evalRunRepository = evalRunRepository;
        _goldsetLoader = goldsetLoader;
        _logger = logger;
    }

    public async Task<EvalRun> RunAsync(string goldset, CancellationToken cancellationToken = default)
    {
        var items = await _goldsetLoader.LoadAsync(goldset, cancellationToken);
        var runStopwatch = Stopwatch.StartNew();

        int top1Hits = 0;
        int top3Hits = 0;
        long totalLatencyMs = 0;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            var result = await _retrieval.RetrieveAsync(
                item.Query, [], isAdmin: true, TopK, cancellationToken);
            sw.Stop();
            totalLatencyMs += sw.ElapsedMilliseconds;

            if (result.IsEmpty) continue;

            var top1 = result.Candidates[0].Entry.SourceId;
            if (string.Equals(top1, item.ExpectedSourceId, StringComparison.OrdinalIgnoreCase))
            {
                top1Hits++;
                top3Hits++;
                continue;
            }

            var inTop3 = result.Candidates.Any(c =>
                string.Equals(c.Entry.SourceId, item.ExpectedSourceId, StringComparison.OrdinalIgnoreCase));
            if (inTop3) top3Hits++;
        }

        runStopwatch.Stop();

        var total = items.Count;
        var dimensions = new EvalDimensions(
            Top1Hit: total == 0 ? 0 : (double)top1Hits / total,
            Top3Hit: total == 0 ? 0 : (double)top3Hits / total,
            AvgLatencyMs: total == 0 ? 0 : (double)totalLatencyMs / total,
            ItemsTotal: total,
            ItemsPassed: top3Hits);

        var composite = ComputeComposite(dimensions);

        var baseline = await _evalRunRepository.GetLatestAsync(goldset, cancellationToken);
        decimal? regression = baseline == null ? null : (decimal)composite - baseline.CompositeScore;

        var evalRun = new EvalRun
        {
            Id = Guid.NewGuid(),
            Goldset = goldset,
            Provider = null,
            Model = KnowledgeIndexConstants.EmbeddingModelName,
            CompositeScore = (decimal)composite,
            DimensionsJson = JsonSerializer.Serialize(dimensions),
            RegressionVsBaseline = regression,
            ItemsTotal = total,
            ItemsPassed = top3Hits,
            DurationMs = (int)runStopwatch.ElapsedMilliseconds,
            CreateTime = DateTime.UtcNow
        };

        await _evalRunRepository.AddAsync(evalRun, cancellationToken);

        _logger.LogInformation(
            "EvalRun {Goldset} completed: composite={Composite:F4}, top1={Top1:F2}, top3={Top3:F2}, items={Items}, regression={Regression}",
            goldset, composite, dimensions.Top1Hit, dimensions.Top3Hit, total, regression);

        return evalRun;
    }

    private static double ComputeComposite(EvalDimensions d)
    {
        var latencyNorm = Math.Clamp(d.AvgLatencyMs / LatencyNormalizerMs, 0.0, 1.0);
        return (Top1Weight * d.Top1Hit)
             + (Top3Weight * d.Top3Hit)
             + (LatencyWeight * (1.0 - latencyNorm));
    }
}
