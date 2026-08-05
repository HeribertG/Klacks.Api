// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Turns the captured wizard runs into the read-only report. Pure by design: it takes the rows and
/// returns the numbers, with no repository, clock or configuration of its own, so every aggregation
/// rule below is directly testable.
/// </summary>
public static class WizardRunCaptureReportBuilder
{
    private const int ChurnHistogramBuckets = 10;
    private const string WarmStartProperty = "warmStart";
    private const string ContextProperty = "context";

    /// <summary>
    /// Builds the report.
    /// </summary>
    /// <param name="captures">Captured runs in the requested window.</param>
    /// <param name="bestTraining">Best feasible benchmark run, or null when none exists.</param>
    /// <param name="recentTrainingCount">Number of recent benchmark runs.</param>
    public static WizardRunCaptureReportDto Build(
        IReadOnlyList<WizardRunCapture> captures,
        WizardTrainingRun? bestTraining,
        int recentTrainingCount)
    {
        var stats = captures
            .GroupBy(c => (c.Engine, c.ApplyKind))
            .OrderBy(g => g.Key.Engine)
            .ThenBy(g => g.Key.ApplyKind)
            .Select(g => BuildEngineStats(g.Key.Engine, g.Key.ApplyKind, [.. g]))
            .ToList();

        var training = new WizardTrainingSummaryDto(
            recentTrainingCount,
            bestTraining?.ConfigJson,
            bestTraining?.Stage2Score,
            bestTraining?.DurationMs,
            bestTraining?.Stage0Violations);

        return new WizardRunCaptureReportDto(captures.Count, stats, training);
    }

    private static WizardRunCaptureEngineStatsDto BuildEngineStats(
        WizardEngine engine, WizardApplyKind applyKind, IReadOnlyList<WizardRunCapture> group)
    {
        var accepted = group.Count(c => c.Outcome == CaptureOutcome.Accepted);
        var rejected = group.Count(c => c.Outcome == CaptureOutcome.Rejected);
        var superseded = group.Count(c => c.Outcome == CaptureOutcome.Superseded);
        var expired = group.Count(c => c.Outcome == CaptureOutcome.Expired);
        var open = group.Count(c => c.Outcome is null);

        var measuredCorrection = group.Where(c => c.CorrectionChurn.HasValue).Select(c => c.CorrectionChurn!.Value).ToList();
        var measuredEvent = group.Where(c => c.EventChurn.HasValue).Select(c => c.EventChurn!.Value).ToList();

        var histogram = new int[ChurnHistogramBuckets];
        foreach (var churn in measuredCorrection)
        {
            histogram[BucketOf(churn)]++;
        }

        var warmStarted = group.Where(c => ReadWarmStart(c.SubScoreJson) == true).ToList();
        var coldStarted = group.Where(c => ReadWarmStart(c.SubScoreJson) == false).ToList();

        return new WizardRunCaptureEngineStatsDto(
            engine.ToString(),
            applyKind.ToString(),
            group.Count,
            accepted,
            rejected,
            superseded,
            expired,
            open,
            AcceptRate(accepted, rejected, superseded, expired),
            measuredCorrection.Count > 0 ? measuredCorrection.Average() : null,
            measuredEvent.Count > 0 ? measuredEvent.Average() : null,
            histogram,
            warmStarted.Count,
            AcceptRate(warmStarted),
            AcceptRate(coldStarted));
    }

    /// <summary>
    /// Share of accepted runs among the RESOLVED ones. A run still waiting for its seal is not a
    /// rejection, so counting it would make every recently planned period look worse than it is.
    /// </summary>
    private static double? AcceptRate(int accepted, int rejected, int superseded, int expired)
    {
        var resolved = accepted + rejected + superseded + expired;
        return resolved == 0 ? null : (double)accepted / resolved;
    }

    private static double? AcceptRate(IReadOnlyList<WizardRunCapture> captures)
        => AcceptRate(
            captures.Count(c => c.Outcome == CaptureOutcome.Accepted),
            captures.Count(c => c.Outcome == CaptureOutcome.Rejected),
            captures.Count(c => c.Outcome == CaptureOutcome.Superseded),
            captures.Count(c => c.Outcome == CaptureOutcome.Expired));

    private static int BucketOf(double churn)
        => Math.Clamp((int)(churn * ChurnHistogramBuckets), 0, ChurnHistogramBuckets - 1);

    /// <summary>
    /// Reads the warm-start flag out of the score blob. Only the token-evolution schema carries it, so
    /// the other engines yield null and stay out of the split rather than being counted as cold. A
    /// malformed blob is unknown too - a report must not fail on one bad row.
    /// </summary>
    private static bool? ReadWarmStart(string subScoreJson)
    {
        if (string.IsNullOrWhiteSpace(subScoreJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(subScoreJson);
            if (!document.RootElement.TryGetProperty(ContextProperty, out var context)
                || !context.TryGetProperty(WarmStartProperty, out var warmStart))
            {
                return null;
            }

            return warmStart.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
