// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Draws a reproducible random sample: the same items in the same order with the same seed always yield the same
/// selection, so a preview and the apply that confirms it pick the same entries. StableSeed derives that seed from the
/// request itself, because a seed handed to the model in a tool result does not survive into the next turn.
/// </summary>
/// <param name="items">The candidates in a stable order; the order is part of what the seed reproduces.</param>
/// <param name="count">Size of the sample; at most all items are returned.</param>
/// <param name="seed">Seed of the random sequence.</param>

using System.Security.Cryptography;
using System.Text;

namespace Klacks.Api.Domain.Common;

public static class SeededSample
{
    private const char PartSeparator = '\u001f';

    public static IReadOnlyList<T> Take<T>(IReadOnlyList<T> items, int count, int seed)
    {
        var take = Math.Clamp(count, 0, items.Count);
        var pool = items.ToArray();
        var random = new Random(seed);

        for (var i = 0; i < take; i++)
        {
            var j = random.Next(i, pool.Length);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        return pool.Take(take).ToList();
    }

    /// <summary>
    /// A non-negative seed that depends only on the given parts, identical across processes and runs.
    /// </summary>
    /// <param name="parts">Everything that defines the selection; null parts count as empty.</param>
    public static int StableSeed(params string?[] parts)
    {
        var text = string.Join(PartSeparator, parts.Select(part => part ?? string.Empty));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return BitConverter.ToInt32(hash, 0) & int.MaxValue;
    }
}
