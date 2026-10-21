// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core value comparer for MultiLanguage columns: compares, hashes and snapshots the per-language values so change tracking detects edits inside the value.
/// </summary>
using Klacks.Api.Domain.Common;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Klacks.Api.Infrastructure.Persistence;

public class MultiLanguageValueComparer : ValueComparer<MultiLanguage>
{
    public MultiLanguageValueComparer() : base(
        (a, b) => AreEqual(a, b),
        v => ComputeHash(v),
        v => Clone(v))
    { }

    private static bool AreEqual(MultiLanguage? a, MultiLanguage? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        var aValues = a.GetAllValues();
        var bValues = b.GetAllValues();
        if (aValues.Count != bValues.Count) return false;
        return aValues.All(kvp => bValues.TryGetValue(kvp.Key, out var val) && val == kvp.Value);
    }

    private static int ComputeHash(MultiLanguage? v)
    {
        if (v is null) return 0;

        var hash = new HashCode();
        foreach (var kvp in v.GetAllValues().OrderBy(x => x.Key))
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value);
        }
        return hash.ToHashCode();
    }

    private static MultiLanguage Clone(MultiLanguage? v)
    {
        if (v is null) return new MultiLanguage();

        var clone = new MultiLanguage();
        foreach (var kvp in v.GetAllValues())
        {
            clone.SetValue(kvp.Key, kvp.Value);
        }
        return clone;
    }
}
