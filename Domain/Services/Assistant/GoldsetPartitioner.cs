// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Splits a shipped goldset into a training and a holdout half. The split is a pure function of the item
/// id - FNV-1a over its UTF-8 bytes (GoldsetItemIdHash) - so the seeder that writes golden cases and the learner that reads
/// eval items reach the same verdict without sharing a row. A random or stored assignment would drift
/// apart the moment one of the two was rebuilt, and the learner would end up optimising against the
/// exact cases its gate replays. An id nobody supplied lands in the holdout half: an unidentifiable item
/// must never become training data.
/// Paraphrase items (id prefix para-) are always train: they are rewordings of train items, and a rewording
/// of a train item in the holdout half would leak training data into the gate.
/// Translated items (i18n-&lt;locale&gt;--&lt;sourceId&gt;, see GoldsetTranslationId) inherit the partition of their
/// source, so the learner never trains on a translation of an item the gate replays as holdout. A malformed
/// translation id names no source and is holdout.
/// </summary>
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class GoldsetPartitioner
{
    private const uint Buckets = 100;
    private const uint TrainBucketCeiling = 70;

    public static string Resolve(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return GoldenCasePartitions.Holdout;
        }

        if (itemId.StartsWith(TurnEvalDefaults.ParaphraseItemIdPrefix, StringComparison.Ordinal))
        {
            return GoldenCasePartitions.Train;
        }

        if (GoldsetTranslationId.IsTranslationId(itemId))
        {
            return GoldsetTranslationId.TryParse(itemId, out _, out var sourceId)
                ? Resolve(sourceId)
                : GoldenCasePartitions.Holdout;
        }

        return Bucket(itemId) < TrainBucketCeiling
            ? GoldenCasePartitions.Train
            : GoldenCasePartitions.Holdout;
    }

    public static bool IsHoldout(string itemId) =>
        Resolve(itemId) == GoldenCasePartitions.Holdout;

    public static bool IsTrain(string itemId) =>
        Resolve(itemId) == GoldenCasePartitions.Train;

    private static uint Bucket(string itemId) => GoldsetItemIdHash.Compute(itemId) % Buckets;
}
