// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The facts a held action was previewed with, in a form the confirmed replay can compare: the kind of fact, the items
/// themselves (ordinal sorted; kept only up to ConfirmationBindingDefaults.MaxStoredItems, otherwise empty) and a
/// SHA-256 fingerprint over kind and all items. Two bindings match when kind and fingerprint are equal; the items only
/// serve to tell the user how the list changed. Built by Of from the very plan the preview was computed from. Equality
/// compares the items by value (a record would compare the list by reference).
/// </summary>
/// <param name="Kind">What the items are (ConfirmationBindingKinds)</param>
/// <param name="Items">The sorted items, or none when there were more than the storage cap</param>
/// <param name="Fingerprint">Lower-case hex SHA-256 over the kind and every item</param>

using System.Security.Cryptography;
using System.Text;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record ConfirmationBinding(string Kind, IReadOnlyList<string> Items, string Fingerprint)
{
    private const char FieldSeparator = '\n';

    public static ConfirmationBinding Of(string kind, IEnumerable<string> items)
    {
        var sorted = items.OrderBy(item => item, StringComparer.Ordinal).ToList();
        var fingerprint = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(kind + FieldSeparator + string.Join(FieldSeparator, sorted))))
            .ToLowerInvariant();
        var stored = sorted.Count <= ConfirmationBindingDefaults.MaxStoredItems ? sorted : new List<string>();
        return new ConfirmationBinding(kind, stored, fingerprint);
    }

    public bool Equals(ConfirmationBinding? other) =>
        other != null
        && string.Equals(Kind, other.Kind, StringComparison.Ordinal)
        && string.Equals(Fingerprint, other.Fingerprint, StringComparison.Ordinal)
        && Items.SequenceEqual(other.Items, StringComparer.Ordinal);

    public override int GetHashCode() => HashCode.Combine(Kind, Fingerprint);

    public bool Matches(ConfirmationBinding other) =>
        string.Equals(Kind, other.Kind, StringComparison.Ordinal)
        && string.Equals(Fingerprint, other.Fingerprint, StringComparison.Ordinal);

    public ConfirmationBindingDiff? DiffTo(ConfirmationBinding current)
    {
        if (Items.Count == 0 || current.Items.Count == 0)
        {
            return null;
        }

        var shown = Items.ToHashSet(StringComparer.Ordinal);
        var now = current.Items.ToHashSet(StringComparer.Ordinal);
        return new ConfirmationBindingDiff(
            Items.Count,
            current.Items.Count,
            now.Count(item => !shown.Contains(item)),
            shown.Count(item => !now.Contains(item)));
    }
}
