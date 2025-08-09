﻿using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Extensions;

public static class QueryableExtensions
{
    public static async Task<HashSet<T>> ToHashSetAsync<T>(this IQueryable<T> source)
    {
        var list = await source.ToListAsync();
        return list.ToHashSet();
    }
}