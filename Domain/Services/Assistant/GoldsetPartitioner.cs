// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Splits a shipped goldset into a training and a holdout half. The split is a pure function of the item
/// id - FNV-1a over its UTF-8 bytes - so the seeder that writes golden cases and the learner that reads
/// eval items reach the same verdict without sharing a row. A random or stored assignment would drift
/// apart the moment one of the two was rebuilt, and the learner would end up optimising against the
/// exact cases its gate replays. An id nobody supplied lands in the holdout half: an unidentifiable item
/// must never become training data.
/// </summary>
using System.Text;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class GoldsetPartitioner
{
    private const uint FnvOffsetBasis = 2166136261;
    private const uint FnvPrime = 16777619;
    private const uint Buckets = 100;
    private const uint TrainBucketCeiling = 70;

    public static string Resolve(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return GoldenCasePartitions.Holdout;
        }

        return Bucket(itemId) < TrainBucketCeiling
            ? GoldenCasePartitions.Train
            : GoldenCasePartitions.Holdout;
    }

    public static bool IsHoldout(string itemId) =>
        Resolve(itemId) == GoldenCasePartitions.Holdout;

    public static bool IsTrain(string itemId) =>
        Resolve(itemId) == GoldenCasePartitions.Train;

    private static uint Bucket(string itemId)
    {
        var hash = FnvOffsetBasis;

        foreach (var octet in Encoding.UTF8.GetBytes(itemId))
        {
            unchecked
            {
                hash ^= octet;
                hash *= FnvPrime;
            }
        }

        return hash % Buckets;
    }
}
