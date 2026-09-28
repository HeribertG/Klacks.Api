// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The gate's measurement of one proposal as stored in proposed_skill_changes.gate_metrics_json, so the
/// verdict is data and not only a sentence in the justification.
/// </summary>
/// <param name="ReferenceEvalRunId">Full eval run the holdout items came from</param>
/// <param name="Model">Model used for every replay</param>
/// <param name="ScorerVersion">Scorer version of the reference run</param>
/// <param name="HoldoutReplays">Holdout items planned</param>
/// <param name="HoldoutMeasured">Holdout items answered on both sides</param>
/// <param name="HoldoutRegressions">Holdout item ids that regressed</param>
/// <param name="HoldoutFixed">Holdout item ids that got fixed</param>
/// <param name="TrainMissesReplayed">Train misses planned</param>
/// <param name="TrainMeasured">Train misses answered on both sides</param>
/// <param name="TrainMissesFixed">Train miss ids the new description fixed</param>
/// <param name="TrainRegressions">Train item ids that got worse</param>
/// <param name="GoldenCaseRegressions">Golden cases the routing gate reported as newly failing</param>
/// <param name="NetGain">Fixed minus regressed over holdout and train items</param>
/// <param name="MinNetGain">The minimum the gate applied</param>
/// <param name="Verdict">One of GoldsetGateVerdicts</param>
/// <param name="IsCalibration">True for a null proposal that only measures replay noise</param>
/// <param name="MeasuredAtUtc">When the measurement ended</param>
/// <param name="UnmeasuredAttempts">Gate runs so far whose paired replay could not be measured; zero otherwise</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record GoldsetGateMetrics(
    Guid ReferenceEvalRunId,
    string Model,
    int ScorerVersion,
    int HoldoutReplays,
    int HoldoutMeasured,
    IReadOnlyList<string> HoldoutRegressions,
    IReadOnlyList<string> HoldoutFixed,
    int TrainMissesReplayed,
    int TrainMeasured,
    IReadOnlyList<string> TrainMissesFixed,
    IReadOnlyList<string> TrainRegressions,
    IReadOnlyList<string> GoldenCaseRegressions,
    int NetGain,
    int MinNetGain,
    string Verdict,
    bool IsCalibration,
    DateTime MeasuredAtUtc,
    int UnmeasuredAttempts = 0);
