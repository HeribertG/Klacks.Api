// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// FNV-1a over the UTF-8 bytes of a goldset item id: a stable, platform-independent hash. GoldsetPartitioner
/// buckets ids with it, and capped selections (translated holdout items in the gate, optimizer evidence) use it
/// as a stable scatter order instead of ordinal id order, which would always favour the same locales or sources.
/// </summary>
/// <param name="itemId">The goldset item id; null is treated as empty</param>
using System.Text;

namespace Klacks.Api.Domain.Services.Assistant;

public static class GoldsetItemIdHash
{
    private const uint FnvOffsetBasis = 2166136261;
    private const uint FnvPrime = 16777619;

    public static uint Compute(string? itemId)
    {
        var hash = FnvOffsetBasis;

        foreach (var octet in Encoding.UTF8.GetBytes(itemId ?? string.Empty))
        {
            unchecked
            {
                hash ^= octet;
                hash *= FnvPrime;
            }
        }

        return hash;
    }
}
