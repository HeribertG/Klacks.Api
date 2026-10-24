// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Gives every analysed order its review key. Orders with the same pattern signature (full-day flag, times, weekdays,
/// executing group and its source, suggested qualifications, rule verdict of the split) form a bundle reviewed once
/// ("B1", the largest first, ties by the earliest start date). An order that already carries an outlier reason, or
/// whose pattern fewer than OrderReviewDefaults.MinBundleSize orders share, is reviewed on its own ("S1", by start
/// date). Owner decision 2026-10-24: bundle by pattern, outliers one by one.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant.OrderReview;

namespace Klacks.Api.Application.Services.Orders.Review;

public static class OrderReviewBundler
{
    public const string TooFewSharePatternReason = "fewer orders than a bundle needs share this pattern";

    public static void AssignKeys(IReadOnlyList<(OrderReviewDraftItem Item, string Signature)> analyzed)
    {
        var bundles = analyzed
            .Where(entry => entry.Item.OutlierReason == null)
            .GroupBy(entry => entry.Signature, StringComparer.Ordinal)
            .ToList();

        foreach (var small in bundles.Where(bundle => bundle.Count() < OrderReviewDefaults.MinBundleSize))
        {
            foreach (var entry in small)
            {
                entry.Item.OutlierReason = TooFewSharePatternReason;
            }
        }

        var orderedBundles = bundles
            .Where(bundle => bundle.Count() >= OrderReviewDefaults.MinBundleSize)
            .OrderByDescending(bundle => bundle.Count())
            .ThenBy(bundle => bundle.Min(entry => entry.Item.FromDate))
            .ToList();

        for (var index = 0; index < orderedBundles.Count; index++)
        {
            var key = OrderReviewDefaults.BundleKeyPrefix + (index + 1).ToString(CultureInfo.InvariantCulture);
            foreach (var entry in orderedBundles[index])
            {
                entry.Item.ReviewKey = key;
            }
        }

        var singles = analyzed
            .Where(entry => entry.Item.OutlierReason != null)
            .OrderBy(entry => entry.Item.FromDate)
            .ThenBy(entry => entry.Item.OrderName, StringComparer.Ordinal)
            .ToList();

        for (var index = 0; index < singles.Count; index++)
        {
            singles[index].Item.ReviewKey =
                OrderReviewDefaults.SingleKeyPrefix + (index + 1).ToString(CultureInfo.InvariantCulture);
        }
    }
}
