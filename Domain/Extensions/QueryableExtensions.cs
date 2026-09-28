// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Extensions;

public static class QueryableExtensions
{
    public static async Task<HashSet<T>> ToHashSetAsync<T>(this IQueryable<T> source)
    {
        var list = await source.ToListAsync();
        return list.ToHashSet();
    }
}
