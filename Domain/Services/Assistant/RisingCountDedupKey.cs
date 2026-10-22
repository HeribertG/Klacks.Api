// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Spells and reads the per-recipient dedup key of an IRisingCountTriggerEvent: the event's DedupKey, a separator and
/// the delivered count ("2026-10-22#7"). Keeping the count in the key keeps every delivered summary a row of its own
/// under the existing unique dedup indexes, and lets the high-water mark of a day be read back from the keys alone.
/// </summary>

using System.Globalization;

namespace Klacks.Api.Domain.Services.Assistant;

public static class RisingCountDedupKey
{
    public const string Separator = "#";

    /// <param name="dedupKey">The event's DedupKey (for the open-order summary: the company day).</param>
    public static string PrefixFor(string dedupKey) => dedupKey + Separator;

    /// <param name="dedupKey">The event's DedupKey.</param>
    /// <param name="count">The count delivered to the recipient.</param>
    public static string For(string dedupKey, int count) =>
        PrefixFor(dedupKey) + count.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// The highest count among the keys spelled by <see cref="For"/> for this DedupKey, or null when none was
    /// delivered. Keys of other DedupKeys and keys without a readable count are ignored.
    /// </summary>
    /// <param name="deliveredKeys">Dedup keys of the recipient's rows of the kind.</param>
    /// <param name="dedupKey">The event's DedupKey.</param>
    public static int? HighestDeliveredCount(IEnumerable<string> deliveredKeys, string dedupKey)
    {
        var prefix = PrefixFor(dedupKey);
        int? highest = null;
        foreach (var key in deliveredKeys)
        {
            if (!key.StartsWith(prefix, StringComparison.Ordinal)
                || !int.TryParse(key.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var count))
            {
                continue;
            }

            highest = highest is null ? count : Math.Max(highest.Value, count);
        }

        return highest;
    }
}
