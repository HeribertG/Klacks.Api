// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of comparing the replay with the old description against the replay with the new one.
/// </summary>
/// <param name="Verdict">One of GoldsetGateVerdicts</param>
/// <param name="HoldoutMeasured">Holdout items answered on both sides</param>
/// <param name="TrainMeasured">Train items answered on both sides</param>
/// <param name="HoldoutFixed">Holdout items missed before and hit after</param>
/// <param name="HoldoutRegressions">Holdout items hit before and missed after</param>
/// <param name="TrainFixed">Train misses the new description fixed</param>
/// <param name="TrainRegressions">Train items hit before and missed after</param>
/// <param name="NetGain">All fixed minus all regressed</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record GoldsetGateOutcome(
    string Verdict,
    int HoldoutMeasured,
    int TrainMeasured,
    IReadOnlyList<string> HoldoutFixed,
    IReadOnlyList<string> HoldoutRegressions,
    IReadOnlyList<string> TrainFixed,
    IReadOnlyList<string> TrainRegressions,
    int NetGain);
