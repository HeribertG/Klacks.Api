// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides a paired replay. An item counts only when both replays answered it; fixed means missed with the old
/// and hit with the new description, regressed the reverse. The proposal is blocked by any regressed holdout
/// item, and passes only when fixed minus regressed over holdout and train items reaches the minimum.
/// The verdict is NotMeasured when no item was answered on both sides, and also when holdout items were planned
/// but none of them was: without a measured holdout item there is no regression check, so a train gain alone
/// does not pass. The check runs per holdout goldset (default and translated): a measured translated item
/// cannot stand in for a German holdout half that went entirely unanswered, nor the reverse. Only a plan
/// without holdout items is decided on the train items alone.
/// Provider output at temperature 0 is not guaranteed to be deterministic, which is why the comparison is
/// between two replays of the same run and not against an older eval run.
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class GoldsetGateJudge
{
    public static GoldsetGateOutcome Judge(
        GoldsetReplayPlan plan,
        IReadOnlyDictionary<GoldsetItemRef, bool?> before,
        IReadOnlyDictionary<GoldsetItemRef, bool?> after,
        int minNetGain)
    {
        var holdout = Compare(plan.HoldoutItems, before, after);
        var train = Compare(plan.TrainItems, before, after);
        var netGain = holdout.Fixed.Count + train.Fixed.Count - holdout.Regressed.Count - train.Regressed.Count;

        var holdoutPlannedButUnmeasured = plan.HoldoutItems
            .GroupBy(item => item.Goldset, StringComparer.Ordinal)
            .Any(group => Compare([.. group], before, after).Measured == 0);
        var verdict = holdout.Measured + train.Measured == 0 || holdoutPlannedButUnmeasured
            ? GoldsetGateVerdicts.NotMeasured
            : holdout.Regressed.Count > 0
                ? GoldsetGateVerdicts.BlockedRegression
                : netGain < minNetGain
                    ? GoldsetGateVerdicts.NoNetGain
                    : GoldsetGateVerdicts.Passed;

        return new GoldsetGateOutcome(
            verdict, holdout.Measured, train.Measured,
            holdout.Fixed, holdout.Regressed, train.Fixed, train.Regressed, netGain);
    }

    private static PairComparison Compare(
        IReadOnlyList<GoldsetItemRef> items,
        IReadOnlyDictionary<GoldsetItemRef, bool?> before,
        IReadOnlyDictionary<GoldsetItemRef, bool?> after)
    {
        var fixedIds = new List<string>();
        var regressedIds = new List<string>();
        var measured = 0;

        foreach (var item in items)
        {
            if (!before.TryGetValue(item, out var old) || !after.TryGetValue(item, out var current)
                || old == null || current == null)
            {
                continue;
            }

            measured++;
            if (old == false && current == true)
            {
                fixedIds.Add(item.ItemId);
            }
            else if (old == true && current == false)
            {
                regressedIds.Add(item.ItemId);
            }
        }

        return new PairComparison(measured, fixedIds, regressedIds);
    }

    private sealed record PairComparison(int Measured, IReadOnlyList<string> Fixed, IReadOnlyList<string> Regressed);
}
