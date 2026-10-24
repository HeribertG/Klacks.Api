// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Guard rails of the skill rerank threshold tuner (owner decision 2026-10-24: learn automatically, inside fixed
/// limits). The bounds of the threshold itself live in KnowledgeIndexConstants and are passed in.
/// </summary>
public static class SkillRerankTuningDefaults
{
    /// <summary>Width of a margin bucket; the candidate thresholds sit on the same grid.</summary>
    public const double BucketWidth = 0.01;

    /// <summary>The largest move of one tuning run, so a burst of unusual turns can only nudge the threshold.</summary>
    public const double MaxStep = 0.01;

    /// <summary>Below this many usable turns nothing is changed: the comparison would be noise.</summary>
    public const int MinObservations = 200;

    /// <summary>Each order (reranked and vector) needs at least this many turns, or the two cannot be compared.</summary>
    public const int MinObservationsPerOrder = 30;

    /// <summary>
    /// Pseudo-observations that pull both orders of a bucket towards the bucket's combined success rate, i.e.
    /// towards "reranking makes no difference here". A bucket must show a difference in real turns before it
    /// counts; three lucky turns cannot decide the threshold. Chosen by a closed-loop simulation (5 % exploration,
    /// 400 turns per run, four ground truths): 60-100 tracked the true threshold best (mean error ~0.003), 10 let it
    /// wander on noise (~0.009), 200 and more made it too slow to follow a real change.
    /// </summary>
    public const double PriorStrength = 60.0;

    /// <summary>
    /// The price of one reranked turn in success units: the cross-encoder costs about 1.6 s per skill pass, so a
    /// bucket is only worth reranking when it raises the success rate by more than this.
    /// </summary>
    public const double RerankCost = 0.02;

    /// <summary>
    /// The threshold only moves when the best candidate beats the current one by at least this many expected
    /// successes per turn whose order the move would change (the turns between the two thresholds). Without it the
    /// threshold would wander on noise wherever the two orders are equal. Half of RerankCost, so a move down that
    /// only saves latency still clears it.
    /// </summary>
    public const double MinGainPerChangedTurn = 0.01;

    /// <summary>Two candidate thresholds closer than this in expected successes count as a tie; the lower one wins,
    /// because it reranks fewer turns and answers faster.</summary>
    public const double TieTolerance = 1e-9;

    /// <summary>Only turns of the last this many days are read, so the threshold follows the current catalogue.</summary>
    public const int WindowDays = 30;

    /// <summary>The most recent turns read per run.</summary>
    public const int MaxTrajectories = 5000;

    /// <summary>Digits the stored threshold is rounded to.</summary>
    public const int ThresholdDecimals = 4;
}
