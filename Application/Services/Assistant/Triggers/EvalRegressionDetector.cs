// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Watches the turn-selection eval for lost ground. Compares the latest two COMPLETED runs of the same
/// goldset, model and scorer version, and speaks up when retrieval or selection fell further than the
/// threshold. Four filters are load-bearing: a partial run covers a different population, a different
/// model is a different measurement, a different scorer version is a different scale, and two runs of
/// different item counts are two populations - both are "full" against the goldset file of their day,
/// so a file that grew between them moves every dimension on bookkeeping rather than on quality.
/// A run persisted before the two dimensions existed carries neither; its drop is read as zero instead
/// of as a collapse from an assumed one, so the first scan after the deploy stays silent. Skipped
/// outright is only a run whose dimensions JSON cannot be parsed at all.
/// Implements IAgentConditionFingerprintSource with the same builder DetectAsync uses, so the ledger
/// resolves the row by itself as soon as a later run recovers.
/// </summary>
/// <param name="evalRunRepository">Supplies the recent completed runs</param>
/// <param name="logger">One line per scan</param>

using System.Text.Json;
using Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed class EvalRegressionDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    private const int RunsPerComparison = 2;

    private readonly IEvalRunRepository _evalRunRepository;
    private readonly ILogger<EvalRegressionDetector> _logger;

    public EvalRegressionDetector(
        IEvalRunRepository evalRunRepository,
        ILogger<EvalRegressionDetector> logger)
    {
        _evalRunRepository = evalRunRepository;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.EvalRegression;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(
        CancellationToken cancellationToken = default)
    {
        var alerts = await BuildAlertsAsync(cancellationToken);

        _logger.LogInformation(
            "EvalRegression scan on goldset {Goldset}: {Count} alert(s)",
            TurnEvalDefaults.DefaultGoldset, alerts.Count);

        return alerts;
    }

    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(
        CancellationToken cancellationToken = default)
    {
        var alerts = await BuildAlertsAsync(cancellationToken);

        return alerts
            .Select(alert => AgentConditionLedgerPolicy.FingerprintFor(alert))
            .ToHashSet(StringComparer.Ordinal);
    }

    private async Task<IReadOnlyList<IAgentTriggerEvent>> BuildAlertsAsync(
        CancellationToken cancellationToken)
    {
        var runs = await _evalRunRepository.ListRecentFullRunsAsync(
            TurnEvalDefaults.DefaultGoldset, EvalRegressionDefaults.RecentRunScanLimit, cancellationToken);

        var alerts = new List<IAgentTriggerEvent>();

        foreach (var group in runs.Where(run => !string.IsNullOrWhiteSpace(run.Model))
                     .GroupBy(run => (run.Model, run.ScorerVersion)))
        {
            var pair = group.OrderByDescending(run => run.CreateTime).Take(RunsPerComparison).ToList();
            if (pair.Count < RunsPerComparison || pair[0].ItemsTotal != pair[1].ItemsTotal)
            {
                continue;
            }

            var current = ReadDimensions(pair[0]);
            var previous = ReadDimensions(pair[1]);
            if (current == null || previous == null)
            {
                continue;
            }

            var retrievalDrop = Drop(previous.RetrievalHit, current.RetrievalHit);
            var selectionDrop = Drop(previous.SelectionHit, current.SelectionHit);

            if (retrievalDrop < EvalRegressionDefaults.DropThreshold
                && selectionDrop < EvalRegressionDefaults.DropThreshold)
            {
                continue;
            }

            alerts.Add(new EvalRegressionTriggerEvent(
                pair[0].Id,
                pair[0].Goldset,
                group.Key.Model!,
                current.RetrievalHit ?? 0.0,
                current.SelectionHit ?? 0.0,
                retrievalDrop,
                selectionDrop));
        }

        return alerts;
    }

    private TurnEvalDimensions? ReadDimensions(EvalRun run)
    {
        try
        {
            return JsonSerializer.Deserialize<TurnEvalDimensions>(run.DimensionsJson);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception, "Dimensions of eval run {RunId} could not be read; the run is skipped", run.Id);
            return null;
        }
    }

    // A dimension nobody measured is not a drop to zero. Both ends have to carry a value, or the pair
    // says nothing.
    private static double Drop(double? before, double? after) =>
        before.HasValue && after.HasValue ? Math.Max(0.0, before.Value - after.Value) : 0.0;
}
