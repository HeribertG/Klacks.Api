// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Computes the next skill rerank threshold from real turns. Every turn carries the margin of its skill pass, the
/// order it used and whether it went well. Because a random share of the Adaptive passes is flipped against the rule
/// (ExplorationRate), both orders occur at every margin, so their success rates can be compared fairly per margin
/// bucket - the controlled comparison the July margin sweep made on the golden set, made here on live traffic.
///
/// For every candidate threshold T on the bucket grid the expected number of successful turns is summed: a bucket
/// below T contributes its turns at the reranked order's success rate minus the latency price of a rerank
/// (RerankCost), a bucket at or above T at the vector order's rate. Both rates of a bucket are shrunk towards the
/// bucket's combined rate (PriorStrength), so a bucket only argues for or against reranking once its own turns show
/// a difference. The best T wins, the lower one on a tie (fewer reranks, faster answers). The threshold moves only
/// when that beats the current one clearly (MinGainPerChangedTurn), then by at most MaxStep and never outside the bounds.
/// Pure and deterministic, so it is tested without a database.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class SkillRerankThresholdCalculator
{
    // Laplace smoothing of a bucket's combined rate: one success in two imagined turns keeps an all-success or
    // all-failure bucket off 1 and 0.
    private const double LaplaceSuccesses = 1.0;
    private const double LaplaceTurns = 2.0;

    /// <summary>
    /// The next threshold, or null when the turns are too few to justify any change. Returns the current value
    /// (rounded) when no candidate is clearly better.
    /// </summary>
    /// <param name="outcomes">The usable turns of the window</param>
    /// <param name="current">The threshold in force</param>
    /// <param name="minThreshold">Lower bound the result never goes below</param>
    /// <param name="maxThreshold">Upper bound the result never goes above</param>
    public static double? Next(
        IReadOnlyList<SkillRerankOutcome> outcomes, double current, double minThreshold, double maxThreshold)
    {
        var rerankedCount = outcomes.Count(o => o.Reranked);
        if (outcomes.Count < SkillRerankTuningDefaults.MinObservations
            || rerankedCount < SkillRerankTuningDefaults.MinObservationsPerOrder
            || outcomes.Count - rerankedCount < SkillRerankTuningDefaults.MinObservationsPerOrder)
        {
            return null;
        }

        var buckets = ToBuckets(outcomes, maxThreshold);
        var best = BestThreshold(buckets, minThreshold, maxThreshold);
        var gain = Score(buckets, best) - Score(buckets, current);
        var changed = ChangedTurns(buckets, best, current);
        var target = changed > 0 && gain >= SkillRerankTuningDefaults.MinGainPerChangedTurn * changed ? best : current;
        var step = Math.Clamp(target - current, -SkillRerankTuningDefaults.MaxStep, SkillRerankTuningDefaults.MaxStep);

        return Math.Round(
            Math.Clamp(current + step, minThreshold, maxThreshold), SkillRerankTuningDefaults.ThresholdDecimals);
    }

    /// <summary>
    /// The candidate threshold with the most expected successful turns; the lowest on a tie. Exposed for tests:
    /// it is the place the learning would settle at, while Next only walks towards it.
    /// </summary>
    public static double BestThreshold(
        IReadOnlyList<SkillRerankOutcome> outcomes, double minThreshold, double maxThreshold) =>
        BestThreshold(ToBuckets(outcomes, maxThreshold), minThreshold, maxThreshold);

    private static double BestThreshold(IReadOnlyList<Bucket> buckets, double minThreshold, double maxThreshold)
    {
        var bestThreshold = minThreshold;
        var bestScore = double.NegativeInfinity;
        for (var index = BucketOf(minThreshold); index <= BucketOf(maxThreshold); index++)
        {
            var threshold = Math.Max(minThreshold, index * SkillRerankTuningDefaults.BucketWidth);
            var score = Score(buckets, threshold);
            if (score > bestScore + SkillRerankTuningDefaults.TieTolerance)
            {
                bestScore = score;
                bestThreshold = threshold;
            }
        }

        return bestThreshold;
    }

    private static double Score(IReadOnlyList<Bucket> buckets, double threshold) =>
        buckets.Sum(b => b.Count * (b.Lower < threshold - SkillRerankTuningDefaults.TieTolerance
            ? b.RerankedRate - SkillRerankTuningDefaults.RerankCost
            : b.VectorRate));

    private static int ChangedTurns(IReadOnlyList<Bucket> buckets, double first, double second)
    {
        var low = Math.Min(first, second) - SkillRerankTuningDefaults.TieTolerance;
        var high = Math.Max(first, second) - SkillRerankTuningDefaults.TieTolerance;
        return buckets.Where(b => b.Lower >= low && b.Lower < high).Sum(b => b.Count);
    }

    private static List<Bucket> ToBuckets(IReadOnlyList<SkillRerankOutcome> outcomes, double maxThreshold)
    {
        var maxBucket = BucketOf(maxThreshold);
        return outcomes
            .GroupBy(o => Math.Min(BucketOf(o.Margin), maxBucket))
            .Select(g =>
            {
                var combined = (g.Count(o => o.Success) + LaplaceSuccesses) / (g.Count() + LaplaceTurns);
                return new Bucket(
                    Lower: g.Key * SkillRerankTuningDefaults.BucketWidth,
                    Count: g.Count(),
                    RerankedRate: ShrunkRate(g.Where(o => o.Reranked).ToList(), combined),
                    VectorRate: ShrunkRate(g.Where(o => !o.Reranked).ToList(), combined));
            })
            .ToList();
    }

    private static int BucketOf(double margin) =>
        (int)Math.Floor(Math.Max(0, margin) / SkillRerankTuningDefaults.BucketWidth + SkillRerankTuningDefaults.TieTolerance);

    private static double ShrunkRate(IReadOnlyList<SkillRerankOutcome> outcomes, double prior) =>
        (outcomes.Count(o => o.Success) + SkillRerankTuningDefaults.PriorStrength * prior)
        / (outcomes.Count + SkillRerankTuningDefaults.PriorStrength);

    private sealed record Bucket(double Lower, int Count, double RerankedRate, double VectorRate);
}
